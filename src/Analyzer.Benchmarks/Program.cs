using Analyzer.Benchmarks.Core;
using Analyzer.Benchmarks.Scenarios;

Console.WriteLine("===============================================================================");
Console.WriteLine("  FaultAnalyzer: System.Text.Json vs Newtonsoft.Json Microbenchmark");
Console.WriteLine("===============================================================================");

var runner = new BenchmarkRunner();
var scenarios = new JsonBenchmarkScenarios(componentCount: 100, linkCount: 150);
var var_scenarios = new VariableJsonBenchmarkScenarios(nodes: 1000, depthLevel: 60);

const int warmup = 1_000;
const int iterations = 10_000;

var benchmarkSuites = new (string Name, Action Action)[]
{
    ("STJ_Write", scenarios.SystemTextJson_Write),
    ("Newtonsoft_Write", scenarios.Newtonsoft_Write),
    ("STJ_Read", scenarios.SystemTextJson_Read),
    ("Newtonsoft_Read", scenarios.Newtonsoft_Read),
    ("STJ_Roundtrip", scenarios.SystemTextJson_Roundtrip),
    ("Newtonsoft_Roundtrip", scenarios.Newtonsoft_Roundtrip),

    // ("STJ_Write", var_scenarios.SystemTextJson_Write),
    // ("Newtonsoft_Write", var_scenarios.Newtonsoft_Write),
    // ("STJ_Read", var_scenarios.SystemTextJson_Read),
    // ("Newtonsoft_Read", var_scenarios.Newtonsoft_Read),
    // ("STJ_Roundtrip", var_scenarios.SystemTextJson_Roundtrip),
    // ("Newtonsoft_Roundtrip", var_scenarios.Newtonsoft_Roundtrip),
};

var results = new List<BenchmarkResult>();

foreach (var (name, action) in benchmarkSuites)
{
    Console.Write($"[Идет замер] {name,-22} ... ");
    var result = runner.Run(name, action, warmupIterations: warmup, measurementIterations: iterations);
    results.Add(result);
    Console.WriteLine($"Готово (Total: {result.TotalExecutionTimeMs:F0} ms)");
}

Console.WriteLine("\n" + new string('-', 105));
Console.WriteLine($"Оверхед бенчмарка: {results[0].HarnessOverheadNs:F2} нс");
Console.WriteLine($"{"Бенчмарк",-22} | {"p50 (мкс)",10} | {"p75 (мкс)",10} | {"p90 (мкс)",10} | {"p95 (мкс)",10} | {"p99 (мкс)",10} | {"Mean (мкс)",10}");
Console.WriteLine(new string('-', 105));

foreach (var res in results)
{
    Console.WriteLine(
        $"{res.BenchmarkName,-22} | " +
        $"{res.P50Ns / 1000.0,10:F2} | " +
        $"{res.P75Ns / 1000.0,10:F2} | " +
        $"{res.P90Ns / 1000.0,10:F2} | " +
        $"{res.P95Ns / 1000.0,10:F2} | " +
        $"{res.P99Ns / 1000.0,10:F2} | " +
        $"{res.MeanNs / 1000.0,10:F2}");
}
Console.WriteLine(new string('-', 105));

// Сохраняем сырые данные всех 60 000 измерений для построения графиков (Шаг 4)
var csvPath = Path.Combine(AppContext.BaseDirectory, "results", "benchmark_raw.csv");
BenchmarkRunner.SaveToCsv(results, csvPath);

Console.WriteLine($"\n[Экспорт] Сырые замеры успешно сохранены в: {csvPath}");