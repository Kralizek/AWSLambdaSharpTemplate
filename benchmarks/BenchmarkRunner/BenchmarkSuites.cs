namespace BenchmarkRunner;

internal static class BenchmarkSuites
{
    public static IReadOnlyDictionary<string, BenchmarkSuite> All { get; } =
        new Dictionary<string, BenchmarkSuite>(StringComparer.OrdinalIgnoreCase)
        {
            ["request"] = new("request", "*RequestBenchmarks*")
        };
}

internal sealed record BenchmarkSuite(string Id, string Filter);
