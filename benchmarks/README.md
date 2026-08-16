# Benchmarks

This directory contains performance benchmarks for the Lambda Template libraries. It has its own solution so benchmark-only projects and dependencies do not become part of the normal product-development solution.

## Layout

- `BenchmarkWorkloads` contains dependency-free application workloads shared by every target.
- `RawSdkTarget` implements the workload as a plain AWS Lambda handler.
- `V5Target` is pinned to `Kralizek.Lambda.Template` 5.0.0 and keeps its original .NET 6 dependency graph isolated from the current source tree.
- `V6Target` references the current projects under `src/` directly.
- `Benchmarks` contains the BenchmarkDotNet runner and comparisons.

V5 and V6 both contain an assembly named `Kralizek.Lambda.Template`. The benchmark host therefore does not reference V5 directly. It loads `V5Target` in an isolated `AssemblyLoadContext` and shares only the neutral `BenchmarkWorkloads` contract. Target loading is performed during benchmark setup and is not part of the measured operation.

## Running locally

Build and run benchmarks in Release configuration from the repository root:

```bash
dotnet build benchmarks/Benchmarks.slnx --configuration Release
dotnet run --project benchmarks/Benchmarks/Benchmarks.csproj --configuration Release --no-build -- --filter "*RequestBenchmarks*"
```

BenchmarkDotNet writes its normal artifacts under `BenchmarkDotNet.Artifacts`.

## Reproducibility

Publishable performance measurements must be collected on controlled local hardware, not GitHub-hosted runners. When sharing results, record at least:

- git commit SHA;
- CPU and memory;
- operating system;
- .NET SDK/runtime version;
- power/performance mode;
- the exact BenchmarkDotNet command and filters used.

Keep the machine on external power where applicable and avoid material background workloads while collecting results. Comparisons over time should use the same reference machine and execution conditions whenever possible.

The benchmark-validation GitHub Actions workflow only restores and builds this solution. It proves that benchmark code remains valid when source or benchmark infrastructure changes; it does not produce performance results.

## Current coverage

The initial request benchmark uses a trivial uppercase workload to compare:

- a plain AWS Lambda handler (`RawSdk`), used as the BenchmarkDotNet baseline;
- the published v5 runtime (`V5`);
- the current v6 source tree (`V6`).

The workload itself is shared so the comparison focuses on invocation-framework overhead rather than different application implementations.
