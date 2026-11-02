namespace Analyzer.Benchmarks.Core;

using System.Diagnostics;
using System.Globalization;
using System.Text;

public class BenchmarkRunner
{
    private readonly double _harnessOverheadNs;

    public BenchmarkRunner()
    {
        _harnessOverheadNs = PrecisionTimer.MeasureHarnessOverheadNs();
    }

    public BenchmarkResult Run(
        string name,
        Action payload,
        int warmupIterations = 1_000,
        int measurementIterations = 10_000,
        Action? teardown = null)
    {
        // 1. Warmup
        for (int i = 0; i < warmupIterations; i++)
        {
            payload();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var result = new BenchmarkResult
        {
            BenchmarkName = name,
            WarmupIterations = warmupIterations,
            MeasurementIterations = measurementIterations,
            HarnessOverheadNs = _harnessOverheadNs
        };

        var startTotal = Stopwatch.GetTimestamp();

        // 2. Measurement
        for (int i = 0; i < measurementIterations; i++)
        {
            var t0 = PrecisionTimer.GetTimestamp();
            payload();
            var t1 = PrecisionTimer.GetTimestamp();

            var elapsedNs = Math.Max(0, PrecisionTimer.ToNanoseconds(t1 - t0) - _harnessOverheadNs);
            var timestampMs = PrecisionTimer.ToNanoseconds(t1 - startTotal) / 1_000_000.0;

            result.RawData.Add(new RawIteration(i + 1, elapsedNs, timestampMs));
        }

        var endTotal = Stopwatch.GetTimestamp();

        // 3. Teardown
        teardown?.Invoke();
        GC.Collect();

        var totalMs = PrecisionTimer.ToNanoseconds(endTotal - startTotal) / 1_000_000.0;
        
        result.CalculateStatistics(totalMs);

        return result;
    }

    public static void SaveToCsv(IEnumerable<BenchmarkResult> results, string filePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        var sb = new StringBuilder();
        sb.AppendLine("BenchmarkName,IterationIndex,ElapsedNs,TimestampMs");

        foreach (var res in results)
        {
            foreach (var raw in res.RawData)
            {
                sb.AppendLine(string.Create(CultureInfo.InvariantCulture,
                    $"{res.BenchmarkName},{raw.Index},{raw.ElapsedNs:F2},{raw.TimestampMs:F4}"));
            }
        }

        File.WriteAllText(filePath, sb.ToString());
    }
}