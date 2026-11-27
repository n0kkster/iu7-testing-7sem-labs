#!/usr/bin/env python3
import argparse
from concurrent.futures import ThreadPoolExecutor
import csv
import http.client
import json
import os
from pathlib import Path
import platform
import socket
import statistics
import subprocess
import tempfile
import time
import urllib.error
import urllib.request
from uuid import uuid4


ROOT = Path(__file__).resolve().parent.parent
COMPOSE = ["docker", "compose", "-f", "docker-compose.lab6.yml"]
SERVICES = ("api", "postgres", "neo4j", "seq", "prometheus", "tempo", "grafana")
MONITORING = ("seq", "prometheus", "tempo", "grafana")
MODES = {
    "baseline": (False, False),
    "tracing": (True, False),
    "verbose": (False, True),
    "tracing-verbose": (True, True),
}
MIB = 1024 ** 2
SAMPLE_INTERVAL = 2


class DockerConnection(http.client.HTTPConnection):
    def connect(self):
        self.sock = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
        self.sock.settimeout(self.timeout)
        docker_host = os.environ.get("DOCKER_HOST", "unix:///var/run/docker.sock")
        if not docker_host.startswith("unix://"):
            raise ValueError("Для сбора статистики нужен локальный Unix-сокет Docker")
        self.sock.connect(docker_host.removeprefix("unix://"))


def container_stats(container):
    connection = DockerConnection("localhost", timeout=10)
    try:
        connection.request("GET", f"/containers/{container}/stats?stream=false&one-shot=true")
        response = connection.getresponse()
        if response.status != 200:
            raise RuntimeError(f"Docker stats: HTTP {response.status} для {container}")
        stats = json.load(response)
    finally:
        connection.close()

    memory = stats["memory_stats"]
    counters = memory["stats"]
    inactive = counters.get("total_inactive_file", counters.get("inactive_file", 0))
    working_set = max(0, memory["usage"] - inactive)
    cpu = stats["cpu_stats"]["cpu_usage"]["total_usage"]
    return cpu, working_set


def wait_http(url, timeout=90):
    deadline = time.monotonic() + timeout
    while True:
        try:
            with urllib.request.urlopen(url, timeout=3) as response:
                return response.read()
        except OSError:
            if time.monotonic() >= deadline:
                raise
            time.sleep(1)


def sample_k6(directory, env, containers):
    summary = directory / "k6.json"
    command = ["k6", "run", "--quiet", "--summary-export", str(summary),
               "scenarios/scenario_c_soak.js"]
    samples = []
    start = time.monotonic()

    def sample(pool):
        data = dict(zip(containers, pool.map(container_stats, containers.values())))
        samples.append((time.monotonic() - start, data))

    with ThreadPoolExecutor(max_workers=len(containers)) as pool, (directory / "k6.log").open("w") as log:
        sample(pool)
        with subprocess.Popen(command, stdout=log, stderr=subprocess.STDOUT, env=env) as k6:
            try:
                while True:
                    try:
                        k6.wait(timeout=SAMPLE_INTERVAL)
                    except subprocess.TimeoutExpired:
                        sample(pool)
                    else:
                        break
                sample(pool)
            finally:
                if k6.poll() is None:
                    k6.terminate()

    with (directory / "resources.csv").open("w", newline="") as csv_file:
        writer = csv.writer(csv_file)
        writer.writerow(["elapsed_s", "service", "total_cpu_ns", "working_set_bytes"])
        for elapsed, data in samples:
            for service, stats in data.items():
                writer.writerow([round(elapsed, 3), service, *stats])

    log_path = directory / "k6.log"
    if not summary.is_file():
        raise SystemExit(f"k6 не сохранил итоговые метрики (код {k6.returncode}). Журнал: {log_path}")
    metrics = json.loads(summary.read_text())["metrics"]
    requests = metrics.get("http_reqs", {}).get("count", 0)
    if not requests:
        raise SystemExit(f"k6 не завершил ни одного HTTP-запроса. Журнал: {log_path}")
    if k6.returncode:
        raise SystemExit(f"k6 завершился с кодом {k6.returncode}. Журнал: {log_path}. Метрики: {summary}")
    results = {}
    for service in SERVICES + ("monitoring", "stack"):
        members = {"monitoring": MONITORING, "stack": SERVICES}.get(service, (service,))
        cpu = sum(samples[-1][1][name][0] - samples[0][1][name][0] for name in members) / 1e9
        memory = [sum(data[name][1] for name in members) / MIB for _, data in samples]
        results[service] = (cpu, cpu * 1000 / requests, statistics.mean(memory), max(memory))

    return {
        "requests": requests,
        "rps": metrics["http_reqs"]["rate"],
        "p95_ms": metrics["http_req_duration"]["p(95)"],
        "failed_rate": metrics["http_req_failed"]["value"],
        "elapsed_s": samples[-1][0] - samples[0][0],
        "resources": results,
    }


def check_trace(directory, trace_id, tracing, tempo_url):
    url = f"{tempo_url}/api/traces/{trace_id}"
    if tracing:
        trace = wait_http(url, timeout=30)
        data = json.loads(trace)
        spans = [
            span
            for batch in data["batches"]
            for scope in batch["scopeSpans"]
            for span in scope["spans"]
        ]
        http_spans = {span["spanId"] for span in spans
                      if span["name"].startswith("POST") and span["name"].endswith("/analysis")}
        if not any(span["name"] == "cascading-failure" and span.get("parentSpanId") in http_spans
                   for span in spans):
            raise RuntimeError(f"В трассе {trace_id} отсутствует спан анализа внутри HTTP-спана")
        (directory / "trace.json").write_bytes(trace)
        return trace_id
    try:
        with urllib.request.urlopen(url, timeout=5):
            raise RuntimeError(f"При выключенной трассировке найдена трасса {trace_id}")
    except urllib.error.HTTPError as error:
        if error.code != 404:
            raise
    return "404"


def write_report(output, modes, env):
    header = [
        "# ЛР 6. Трассировка и логирование FaultAnalyzer",
        "",
        f"Сценарий `soak0` · {env['SOAK_DURATION']} · {env['SOAK_VUS']} VU · без искусственных ошибок.",
        "",
        "Каждый VU выполняет чтение систем, чтение компонентов и анализ каскадного сбоя.",
        "Все режимы используют один тестовый граф и один JWT нагрузочного пользователя.",
        "Токен получен при подготовке данных. В измеряемом трафике запросов логина нет.",
        "Перед каждым замером API пересоздаётся и прогревается в течение 10 секунд.",
        "Настройки БД, каналов логирования и состав сервисов мониторинга одинаковы во всех режимах.",
        f"Стенд замера — {platform.system()} {platform.machine()}, логических CPU {os.cpu_count()}.",
        "",
        "| Режим | Трассировка | Логирование | Запросы | Запросов/с | p95, мс | Ошибки | Трасса k6 в Tempo |",
        "|---|---|---|---:|---:|---:|---:|---|",
    ]
    lines = header
    for mode, result in modes.items():
        tracing, verbose = MODES[mode]
        trace = f"[{result['trace']}]({mode}/trace.json)" if tracing else "HTTP 404"
        logging = "Debug + Info ASP.NET Core и EF Core" if verbose else "По умолчанию"
        lines.append(f"| {mode} | {'Вкл.' if tracing else 'Выкл.'} | {logging} | "
                     f"{result['requests']} | {result['rps']:.1f} | {result['p95_ms']:.2f} | "
                     f"{result['failed_rate']:.2%} | {trace} |")

    lines += [
        "", "## Потребление ресурсов", "",
        "CPU — разность накопленных счётчиков Docker в CPU-секундах между началом и концом прогона.",
        "Память — рабочий набор контейнера за вычетом неактивного файлового кеша.",
        "Средние и максимумы рассчитаны по снимкам с интервалом около двух секунд.",
        "Для суммы по контейнерам память складывается в каждом снимке.",
        "Время CPU нормировано на 1000 HTTP-запросов, поскольку k6 поддерживает число VU и частота запросов может меняться.",
        "",
        "| Режим | Сервис | CPU, с | CPU-с / 1000 запросов | Память средняя, МиБ | Память макс., МиБ |",
        "|---|---|---:|---:|---:|---:|",
    ]
    for mode, result in modes.items():
        for service, stats in result["resources"].items():
            group = {"monitoring": "Мониторинг суммарно", "stack": "Стенд суммарно"}.get(service, service)
            values = " | ".join(f"{value:.2f}" for value in stats)
            lines.append(f"| {mode} | {group} | {values} |")

    comparisons = (
        ("Эффект трассировки при стандартном логировании", "baseline", "tracing"),
        ("Эффект трассировки при расширенном логировании", "verbose", "tracing-verbose"),
        ("Эффект расширенного логирования без трассировки", "baseline", "verbose"),
        ("Эффект расширенного логирования с трассировкой", "tracing", "tracing-verbose"),
    )
    lines += [
        "", "## Разницы между режимами", "",
        "| Сравнение | API Δ CPU-с / 1000 запросов | API Δ средней памяти, МиБ | Мониторинг Δ CPU, с | Мониторинг Δ средней памяти, МиБ |",
        "|---|---:|---:|---:|---:|",
    ]
    for label, control, experiment in comparisons:
        if control in modes and experiment in modes:
            before = modes[control]["resources"]
            after = modes[experiment]["resources"]
            changes = [after[service][i] - before[service][i]
                       for service, i in (("api", 1), ("api", 2), ("monitoring", 0), ("monitoring", 2))]
            values = " | ".join(f"{value:+.2f}" for value in changes)
            lines.append(f"| {label} | {values} |")

    lines += [
        "",
        "Нагрузка измерена по одному разу на режим. Разницы отражают этот запуск и включают влияние кешей и состояния фоновых сервисов.",
        "В замеры включены контейнеры стенда. k6 работает на том же хосте и не входит в таблицу.",
        "",
    ]
    (output / "report.md").write_text("\n".join(lines))
    (output / "summary.json").write_text(json.dumps(modes, indent=2) + "\n")


def main():
    parser = argparse.ArgumentParser(description="Сравнение четырёх режимов observability на soak0")
    parser.add_argument("--output", type=Path, default=Path("results/lab6"))
    parser.add_argument("--no-build", action="store_true", help="Использовать уже собранный образ API")
    args = parser.parse_args()
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    if any((output / mode).exists() for mode in MODES):
        parser.error(f"В {output} уже есть прогоны. Укажите новую папку через --output.")
    os.chdir(ROOT)

    env = os.environ.copy()
    env.update(LAB6_TRACING="false", LAB6_VERBOSE="false", ERROR_RATE="0")
    env.setdefault("SOAK_DURATION", "1m")
    env.setdefault("SOAK_VUS", "100")
    api_url = f"http://127.0.0.1:{env.get('LAB6_API_PORT', '1666')}"
    tempo_url = f"http://127.0.0.1:{env.get('LAB6_TEMPO_PORT', '3200')}"
    build = "--no-build" if args.no_build else "--build"
    subprocess.run([*COMPOSE, "up", "-d", build], env=env, check=True)
    wait_http(f"{api_url}/api/health/ready")
    wait_http(f"{tempo_url}/ready")

    results = {}
    with tempfile.TemporaryDirectory(prefix="analyzer-lab6-") as temporary:
        context = str(Path(temporary) / "context.json")
        token = subprocess.check_output(["python3", "scripts/seed-lab6.py", api_url, context],
                                        text=True, env=env).strip()
        env.update(LAB6_CONTEXT=context, LAB6_TOKEN=token)
        env.pop("LAB6_TRACEPARENT", None)
        for mode, (tracing, verbose) in MODES.items():
            print(f"soak0: {mode}", flush=True)
            directory = output / mode
            directory.mkdir()
            env.update(LAB6_TRACING=str(tracing).lower(), LAB6_VERBOSE=str(verbose).lower())
            subprocess.run([*COMPOSE, "up", "-d", "--no-deps", "--no-build", "--force-recreate", "api"],
                           check=True, env=env)
            wait_http(f"{api_url}/api/health/ready")
            warmup = directory / "warmup.log"
            with warmup.open("w") as log:
                process = subprocess.run(["k6", "run", "--quiet", "scenarios/scenario_c_soak.js"],
                                         env={**env, "SOAK_DURATION": "10s"},
                                         stdout=log, stderr=subprocess.STDOUT)
            if process.returncode:
                raise SystemExit(f"Прогрев завершился с кодом {process.returncode}. Журнал: {warmup}")
            containers = {
                service: subprocess.check_output([*COMPOSE, "ps", "-q", service], env=env, text=True).strip()
                for service in SERVICES
            }
            trace_id = uuid4().hex
            traceparent = f"00-{trace_id}-{uuid4().hex[:16]}-01"
            result = sample_k6(directory, {**env, "LAB6_TRACEPARENT": traceparent}, containers)
            result["trace"] = check_trace(directory, trace_id, tracing, tempo_url)
            results[mode] = result
            write_report(output, results, env)

    print(f"Отчёт: {output / 'report.md'}")


if __name__ == "__main__":
    main()
