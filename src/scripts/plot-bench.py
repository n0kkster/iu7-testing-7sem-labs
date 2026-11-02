import os
import glob
import pandas as pd
import numpy as np
import matplotlib.pyplot as plt

csv_candidates = glob.glob("**/results/benchmark_raw.csv", recursive=True) + ["benchmark_raw.csv"]
if not csv_candidates:
    print("Файл benchmark_raw.csv не найден.")
    exit(1)

csv_path = csv_candidates[0]
print(f"📊 Загрузка данных из: {csv_path}")

df = pd.read_csv(csv_path)

df['ElapsedUs'] = df['ElapsedNs'] / 1000.0

os.makedirs("plots", exist_ok=True)

scenarios = [
    ("Write", "STJ_Write", "Newtonsoft_Write"),
    ("Read", "STJ_Read", "Newtonsoft_Read"),
    ("Roundtrip", "STJ_Roundtrip", "Newtonsoft_Roundtrip")
]

colors = {
    'STJ': '#1f77b4',
    'Newtonsoft': '#ff7f0e'
}

fig, axes = plt.subplots(3, 1, figsize=(12, 10), sharex=True)
fig.suptitle("Задержка выполнения операций во времени", fontsize=14, fontweight='bold')

for idx, (title, stj_name, newton_name) in enumerate(scenarios):
    ax = axes[idx]
    stj_data = df[df['BenchmarkName'] == stj_name]
    newton_data = df[df['BenchmarkName'] == newton_name]
    
    ax.plot(stj_data['IterationIndex'], stj_data['ElapsedUs'], label='System.Text.Json', color=colors['STJ'], alpha=0.6, linewidth=0.8)
    ax.plot(newton_data['IterationIndex'], newton_data['ElapsedUs'], label='Newtonsoft.Json', color=colors['Newtonsoft'], alpha=0.6, linewidth=0.8)
    
    ax.set_title(f"Сценарий: {title}", fontsize=11, fontweight='bold')
    ax.set_ylabel("Задержка (мкс)")
    ax.grid(True, linestyle='--', alpha=0.5)
    ax.legend(loc='upper right')

axes[2].set_xlabel("Номер итерации (порядок выполнения)")
plt.tight_layout()
plt.savefig("plots/01_latency_over_time.png", dpi=300)
plt.close()
print("Построен график задержки во времени: plots/01_latency_over_time.png")

fig, axes = plt.subplots(1, 3, figsize=(16, 5))
fig.suptitle("Распределение задержек по перцентилям", fontsize=14, fontweight='bold')

percentile_ranks = np.linspace(0, 99.9, 500)
target_percentiles = [50, 75, 90, 95, 99]

for idx, (title, stj_name, newton_name) in enumerate(scenarios):
    ax = axes[idx]
    stj_vals = df[df['BenchmarkName'] == stj_name]['ElapsedUs'].values
    newton_vals = df[df['BenchmarkName'] == newton_name]['ElapsedUs'].values
    
    stj_pcts = np.percentile(stj_vals, percentile_ranks)
    newton_pcts = np.percentile(newton_vals, percentile_ranks)
    
    ax.plot(percentile_ranks, stj_pcts, label='System.Text.Json', color=colors['STJ'], linewidth=2)
    ax.plot(percentile_ranks, newton_pcts, label='Newtonsoft.Json', color=colors['Newtonsoft'], linewidth=2)
    
    for p in target_percentiles:
        stj_p = np.percentile(stj_vals, p)
        ax.axvline(x=p, color='gray', linestyle=':', alpha=0.5)
        ax.scatter([p], [stj_p], color=colors['STJ'], s=25)
    
    ax.set_title(f"Сценарий: {title}", fontsize=11, fontweight='bold')
    ax.set_xlabel("Перцентиль (%)")
    ax.set_ylabel("Задержка (мкс)")
    ax.set_xticks(target_percentiles)
    ax.grid(True, linestyle='--', alpha=0.5)
    ax.legend()

plt.tight_layout()
plt.savefig("plots/02_percentiles_distribution.png", dpi=300)
plt.close()
print("Построен график перцентилей: plots/02_percentiles_distribution.png")

fig, axes = plt.subplots(1, 3, figsize=(16, 5))
fig.suptitle("Гистограмма распределения плотности задержек", fontsize=14, fontweight='bold')

for idx, (title, stj_name, newton_name) in enumerate(scenarios):
    ax = axes[idx]
    stj_vals = df[df['BenchmarkName'] == stj_name]['ElapsedUs'].values
    newton_vals = df[df['BenchmarkName'] == newton_name]['ElapsedUs'].values
    
    limit = max(np.percentile(stj_vals, 99.5), np.percentile(newton_vals, 99.5))
    
    bins = np.linspace(0, limit, 50)
    
    ax.hist(stj_vals[stj_vals <= limit], bins=bins, alpha=0.5, label='System.Text.Json', color=colors['STJ'], density=True)
    ax.hist(newton_vals[newton_vals <= limit], bins=bins, alpha=0.5, label='Newtonsoft.Json', color=colors['Newtonsoft'], density=True)
    
    ax.set_title(f"Сценарий: {title}", fontsize=11, fontweight='bold')
    ax.set_xlabel("Задержка (мкс)")
    ax.set_ylabel("Плотность распределения")
    ax.grid(True, linestyle='--', alpha=0.5)
    ax.legend()

plt.tight_layout()
plt.savefig("plots/03_latency_histogram.png", dpi=300)
plt.close()
print("Построен график плотности задержек: plots/03_latency_histogram.png")

print("\n" + "="*80)
print("  СВОДНЫЕ ДАННЫЕ ДЛЯ ОТЧЕТА")
print("="*80)
print(f"Количество итераций прогрева:    1 000 на каждый тест")
print(f"Количество рабочих испытаний:    10 000 на каждый тест")
print("-"*80)
print(f"{'Тест':<20} | {'p50 (мкс)':<9} | {'p75 (мкс)':<9} | {'p90 (мкс)':<9} | {'p95 (мкс)':<9} | {'p99 (мкс)':<9}")
print("-"*80)

for name in df['BenchmarkName'].unique():
    sub = df[df['BenchmarkName'] == name]['ElapsedUs'].values
    p50, p75, p90, p95, p99 = np.percentile(sub, [50, 75, 90, 95, 99])
    print(f"{name:<20} | {p50:<9.2f} | {p75:<9.2f} | {p90:<9.2f} | {p95:<9.2f} | {p99:<9.2f}")
print("="*80)