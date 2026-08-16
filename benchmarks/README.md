# Benchmarks

This directory contains performance benchmarks for the Lambda Template libraries. It has its own solution so benchmark-only projects and dependencies do not become part of the normal product-development solution.

## Layout

- `BenchmarkRunner` provides the standard way to execute named benchmark suites and collect reproducible results.
- `BenchmarkWorkloads` contains dependency-free application workloads shared by every target.
- `RawSdkTarget` implements the workload as a plain AWS Lambda handler.
- `V5Target` is pinned to `Kralizek.Lambda.Template` 5.0.0 and keeps its original .NET 6 dependency graph isolated from the current source tree.
- `V6Target` references the current projects under `src/` directly.
- `Benchmarks` contains the BenchmarkDotNet benchmark host and comparisons.

V5 and V6 both contain an assembly named `Kralizek.Lambda.Template`. The benchmark host therefore does not reference V5 directly. It loads `V5Target` in an isolated `AssemblyLoadContext` and shares only the neutral `BenchmarkWorkloads` contract. Target loading is performed during benchmark setup and is not part of the measured operation.

## Toolchain

The benchmark subtree pins .NET SDK 10.0.400 through `benchmarks/global.json` and uses C# 14 for every benchmark project, regardless of target framework. Target frameworks describe the runtime/API surface being exercised; they do not select an older compiler for benchmark source code.

Run benchmark commands from the `benchmarks` directory so the benchmark-specific SDK pin is applied.

## Standard runner

Use `BenchmarkRunner` when collecting results that should be retained or compared later. Suites are stable semantic identifiers; the runner owns the mapping from a suite to the BenchmarkDotNet filter used to execute it.

List available suites:

```bash
cd benchmarks
dotnet run --project BenchmarkRunner/BenchmarkRunner.csproj -- --list
```

Run the request suite:

```bash
dotnet run --project BenchmarkRunner/BenchmarkRunner.csproj -- request
```

The runner:

- requires a clean Git working tree by default so the recorded commit identifies the code being measured;
- builds the benchmark host in Release configuration;
- executes BenchmarkDotNet with the suite filter and GitHub Markdown, CSV, and HTML exporters;
- captures Git, machine, .NET, and GitHub Actions metadata in `metadata.json`;
- creates `README.md` as the run homepage by prepending run metadata to the BenchmarkDotNet GitHub Markdown report;
- preserves the BenchmarkDotNet output under the run's `artifacts` directory.

Use `--allow-dirty` only for exploratory measurements that intentionally do not correspond exactly to the recorded commit.

On machines where the power/performance mode is relevant, set `BENCHMARK_POWER_MODE` before running so the value is recorded in the manifest and homepage, for example:

```bash
BENCHMARK_POWER_MODE=performance dotnet run --project BenchmarkRunner/BenchmarkRunner.csproj -- request
```

## Result layout

Collected runs are stored by suite and run identity, not by machine name:

```text
results/
  request/
    2026-08-16T150149Z-8a2a4ce0/
      README.md
      metadata.json
      artifacts/
```

`README.md` is the human-readable homepage for a run. `metadata.json` is the machine-readable manifest intended for future indexing and aggregation. Machine identity and execution-provider details remain metadata so local and hosted-runner results use the same directory structure.

## Direct BenchmarkDotNet execution

For benchmark development and troubleshooting, the BenchmarkDotNet host can still be run directly:

```bash
cd benchmarks
dotnet --version # must report 10.0.400
dotnet build Benchmarks.slnx --configuration Release
dotnet run --project Benchmarks/Benchmarks.csproj --configuration Release --no-build -- --filter "*RequestBenchmarks*"
```

Direct execution writes BenchmarkDotNet's normal artifacts and does not create the standardized result manifest or run homepage.

## Reproducibility

Publishable performance measurements should be collected on controlled hardware. The standard runner automatically records the git commit and state, CPU where available, memory limit visible to .NET, operating system, architecture, .NET SDK/runtime, suite/filter, timestamp, and execution provider. BenchmarkDotNet's generated report provides its own detailed runtime and environment description.

Keep the machine on external power where applicable and avoid material background workloads while collecting results. Comparisons over time should use the same reference machine and execution conditions whenever possible.

The benchmark-validation GitHub Actions workflow only restores and builds this solution. It verifies the pinned SDK before building. It proves that benchmark code remains valid when source or benchmark infrastructure changes; it does not produce performance results.

## Current coverage

The initial request benchmark uses a trivial uppercase workload to compare:

- a plain AWS Lambda handler (`RawSdk`), used as the BenchmarkDotNet baseline;
- the published v5 runtime (`V5`);
- the current v6 source tree (`V6`).

The workload itself is shared so the comparison focuses on invocation-framework overhead rather than different application implementations.
