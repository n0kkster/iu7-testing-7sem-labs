namespace Analyzer.Benchmarks.Core;

using System.Diagnostics;
using System.Runtime.CompilerServices;

public static class PrecisionTimer
{
    private static readonly double TicksToNanoseconds = 1_000_000_000.0 / Stopwatch.Frequency;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetTimestamp() => Stopwatch.GetTimestamp();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ToNanoseconds(long ticks) => ticks * TicksToNanoseconds;

    public static double MeasureHarnessOverheadNs(int batches = 50_000)
    {
        for (int i = 0; i < 5_000; i++)
        {
            _ = Stopwatch.GetTimestamp();
        }

        long totalTicks = 0;

        for (int i = 0; i < batches; i++)
        {
            var a0 = Stopwatch.GetTimestamp(); var a1 = Stopwatch.GetTimestamp();
            var b0 = Stopwatch.GetTimestamp(); var b1 = Stopwatch.GetTimestamp();
            var c0 = Stopwatch.GetTimestamp(); var c1 = Stopwatch.GetTimestamp();
            var d0 = Stopwatch.GetTimestamp(); var d1 = Stopwatch.GetTimestamp();
            var e0 = Stopwatch.GetTimestamp(); var e1 = Stopwatch.GetTimestamp();
            var f0 = Stopwatch.GetTimestamp(); var f1 = Stopwatch.GetTimestamp();
            var g0 = Stopwatch.GetTimestamp(); var g1 = Stopwatch.GetTimestamp();
            var h0 = Stopwatch.GetTimestamp(); var h1 = Stopwatch.GetTimestamp();

            totalTicks += (a1 - a0) + (b1 - b0) + (c1 - c0) + (d1 - d0) +
                          (e1 - e0) + (f1 - f0) + (g1 - g0) + (h1 - h0);
        }

        return ToNanoseconds(totalTicks) / (batches * 8);
    }
}