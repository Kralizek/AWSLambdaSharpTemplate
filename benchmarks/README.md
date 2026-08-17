# Benchmarks

This directory contains performance benchmarks for the Lambda Template libraries. It has its own solution so benchmark-only projects and dependencies do not become part of the normal product-development solution.

## Layout

- `BenchmarkWorkloads` contains dependency-free application workloads shared by every target.
- `RawSdkTarget` implements the workloads as plain AWS Lambda handlers.
- `V5Target` is pinned to the published v5 packages and keeps its original .NET 6 dependency graph isolated from the current source tree.
- `V6Target` references the current projects under `src/` directly.
- `Benchmarks` contains the BenchmarkDotNet benchmark host and comparisons.

The benchmark host loads the raw SDK, v5, and v6 targets through separate collectible `AssemblyLoadContext` instances and shares only the neutral `BenchmarkWorkloads` contract. Target loading happens during benchmark setup and is not part of the measured operation.

## Toolchain

The benchmark subtree pins .NET SDK 10.0.400 through `benchmarks/global.json` and uses C# 14 for every benchmark project, regardless of target framework.

Run benchmark commands from the `benchmarks` directory so the benchmark-specific SDK pin is applied.

## Running benchmarks

Benchmark execution uses BenchmarkDotNet directly. Build the benchmark solution first:

```bash
cd benchmarks
dotnet --version # must report 10.0.400
dotnet build Benchmarks.slnx --configuration Release
```

Run all benchmarks:

```bash
dotnet run --project Benchmarks/Benchmarks.csproj --configuration Release --no-build
```

Use BenchmarkDotNet filters when developing or investigating a specific benchmark suite:

```bash
dotnet run --project Benchmarks/Benchmarks.csproj --configuration Release --no-build -- --filter "*RequestBenchmarks*"
dotnet run --project Benchmarks/Benchmarks.csproj --configuration Release --no-build -- --filter "*SqsBenchmarks*"
```

All normal BenchmarkDotNet command-line options remain available. BenchmarkDotNet owns benchmark discovery, filtering, exporters, jobs, diagnosers, and artifact generation.

## Release benchmark history

`.github/workflows/release-benchmarks.yml` runs the BenchmarkDotNet host directly and requests its standard JSON exporter. The raw `BenchmarkDotNet.Artifacts` directory is retained as a GitHub Actions artifact for troubleshooting.

For real release runs, the workflow passes those JSON results to `martincostello/benchmarkdotnet-results-publisher`, which appends the summarized measurements to the repository's `gh-pages` branch. This gives the project a durable release-to-release history without maintaining a custom result schema, metadata manifest, directory convention, or release ZIP format.

Manual executions of the benchmark workflow still produce the raw Actions artifact but do not publish into the historical data set.

The published data is intended for relative trend analysis. GitHub-hosted runners are not controlled benchmark hardware, so the history should not be treated as an absolute or regression-gating baseline. BenchmarkDotNet's own reports include runtime and environment information useful when interpreting individual runs.

## Reproducibility

Publishable performance measurements that require stable absolute comparisons should still be collected on controlled hardware. Keep the machine on external power where applicable and avoid material background workloads while collecting results.

The benchmark-validation GitHub Actions workflow only restores and builds this solution. It verifies the pinned SDK before building. It proves that benchmark code remains valid when source or benchmark infrastructure changes; it does not produce performance results.

## Current coverage

### Request functions

The request benchmark uses a trivial uppercase workload to compare:

- a plain AWS Lambda handler (`RawSdk`), used as the BenchmarkDotNet baseline;
- the published v5 runtime (`V5`);
- the current v6 source tree (`V6`).

The workload itself is shared so the comparison focuses on invocation-framework overhead rather than different application implementations.

### SQS functions

The SQS benchmark measures batches of 1, 10, and 100 records and compares:

- a plain AWS Lambda SQS handler (`RawSdk`), used as the BenchmarkDotNet baseline;
- the published v5 typed SQS function (`V5Typed`);
- the current v6 raw SQS function (`V6Raw`);
- the current v6 typed SQS function (`V6Typed`).

Every target receives an equivalent pre-built SQS envelope. Envelope construction and target loading happen during benchmark setup so the measured operation focuses on dispatch, decoding, record handling, and response construction.
