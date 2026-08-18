using System;
using System.Linq;

using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Running;

namespace Benchmarks;

internal enum BenchmarkProfile
{
    Full,
    Ci,
    Stress
}

internal static class BenchmarkProfiles
{
    public static BenchmarkProfile Parse(string value) =>
        value.ToLowerInvariant() switch
        {
            "full" => BenchmarkProfile.Full,
            "ci" => BenchmarkProfile.Ci,
            "stress" => BenchmarkProfile.Stress,
            _ => throw new ArgumentException($"Unknown benchmark profile '{value}'. Expected one of: full, ci, stress.")
        };

    public static IFilter CreateFilter(BenchmarkProfile profile) =>
        new ProfileFilter(profile);

    private sealed class ProfileFilter(BenchmarkProfile profile) : IFilter
    {
        private const string BatchSizeParameter = "BatchSize";
        private const string FailurePercentParameter = "FailurePercent";

        public bool Predicate(BenchmarkCase benchmarkCase)
        {
            if (profile == BenchmarkProfile.Full)
            {
                return true;
            }

            var benchmarkType = benchmarkCase.Descriptor.Type;

            return profile switch
            {
                BenchmarkProfile.Ci => IsCiBenchmark(benchmarkType, benchmarkCase),
                BenchmarkProfile.Stress => IsStressBenchmark(benchmarkType, benchmarkCase),
                _ => false
            };
        }

        private static bool IsCiBenchmark(Type benchmarkType, BenchmarkCase benchmarkCase) =>
            benchmarkType == typeof(RequestBenchmarks) ||
            benchmarkType == typeof(SqsBenchmarks) && HasParameter(benchmarkCase, BatchSizeParameter, 10) ||
            benchmarkType == typeof(AsyncSqsBenchmarks) && HasParameter(benchmarkCase, BatchSizeParameter, 10) ||
            benchmarkType == typeof(SqsReturnedFailureBenchmarks) && HasParameter(benchmarkCase, FailurePercentParameter, 10) ||
            benchmarkType == typeof(SqsExceptionFailureBenchmarks) && HasParameter(benchmarkCase, FailurePercentParameter, 10) ||
            benchmarkType == typeof(NestedSqsSnsS3Benchmarks) && HasParameter(benchmarkCase, BatchSizeParameter, 1) ||
            benchmarkType == typeof(NestedAsyncSqsSnsS3Benchmarks) && HasParameter(benchmarkCase, BatchSizeParameter, 1);

        private static bool IsStressBenchmark(Type benchmarkType, BenchmarkCase benchmarkCase) =>
            benchmarkType == typeof(SqsBenchmarks) && HasParameter(benchmarkCase, BatchSizeParameter, 100) ||
            benchmarkType == typeof(AsyncSqsBenchmarks) && HasParameter(benchmarkCase, BatchSizeParameter, 100) ||
            benchmarkType == typeof(SqsReturnedFailureBenchmarks) && HasParameter(benchmarkCase, FailurePercentParameter, 50, 100) ||
            benchmarkType == typeof(SqsExceptionFailureBenchmarks) && HasParameter(benchmarkCase, FailurePercentParameter, 50, 100) ||
            benchmarkType == typeof(NestedSqsSnsS3Benchmarks) && HasParameter(benchmarkCase, BatchSizeParameter, 10) ||
            benchmarkType == typeof(NestedAsyncSqsSnsS3Benchmarks) && HasParameter(benchmarkCase, BatchSizeParameter, 10);

        private static bool HasParameter(BenchmarkCase benchmarkCase, string name, params int[] expectedValues)
        {
            var parameter = benchmarkCase.Parameters.Items.SingleOrDefault(item => item.Name == name);
            return parameter?.Value is int value && expectedValues.Contains(value);
        }
    }
}
