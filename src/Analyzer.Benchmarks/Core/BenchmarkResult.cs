namespace Analyzer.Benchmarks.Core;

public record RawIteration(int Index, double ElapsedNs, double TimestampMs);

public class BenchmarkResult
{
    public required string BenchmarkName { get; init; }
    public required int WarmupIterations { get; init; }
    public required int MeasurementIterations { get; init; }
    public required double HarnessOverheadNs { get; init; }

    public double TotalExecutionTimeMs { get; set; }

    public double MinNs { get; private set; }
    public double MaxNs { get; private set; }
    public double MeanNs { get; private set; }
    public double StdDevNs { get; private set; }

    public double P50Ns { get; private set; }
    public double P75Ns { get; private set; }
    public double P90Ns { get; private set; }
    public double P95Ns { get; private set; }
    public double P99Ns { get; private set; }

    public List<RawIteration> RawData { get; } = [];

    public void CalculateStatistics(double totalExecutionTimeMs)
    {
        TotalExecutionTimeMs = totalExecutionTimeMs;
        if (RawData.Count == 0) return;

        var sorted = RawData.Select(x => x.ElapsedNs).OrderBy(x => x).ToArray();

        MinNs = sorted[0];
        MaxNs = sorted[^1];
        MeanNs = sorted.Average();

        var sumSquares = sorted.Sum(d => Math.Pow(d - MeanNs, 2));
        StdDevNs = Math.Sqrt(sumSquares / sorted.Length);

        P50Ns = GetPercentile(sorted, 0.50);
        P75Ns = GetPercentile(sorted, 0.75);
        P90Ns = GetPercentile(sorted, 0.90);
        P95Ns = GetPercentile(sorted, 0.95);
        P99Ns = GetPercentile(sorted, 0.99);
    }

    private static double GetPercentile(double[] sortedData, double percentile)
    {
        var n = sortedData.Length;
        var index = (int)Math.Ceiling(percentile * n) - 1;
        return sortedData[Math.Clamp(index, 0, n - 1)];
    }
}