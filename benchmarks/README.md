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

The benchmark host supports three execution profiles through `--profile`:

- `full` is the default and runs the complete benchmark matrix. This is the normal choice for local and controlled-hardware measurements.
- `ci` runs a small representative set intended for GitHub-hosted runners: request, synchronous SQS batch 10, asynchronous SQS batch 10, returned and exception failures at 10%, and synchronous/asynchronous nested SQS to SNS to S3 with batch 1.
- `stress` runs the heavier scaling and edge cases: synchronous/asynchronous SQS batch 100, returned and exception failures at 50% and 100%, and synchronous/asynchronous nested SQS to SNS to S3 with batch 10.

Examples:

```bash
dotnet run --project Benchmarks/Benchmarks.csproj --configuration Release --no-build -- --profile ci
dotnet run --project Benchmarks/Benchmarks.csproj --configuration Release --no-build -- --profile stress
```

Profiles compose with normal BenchmarkDotNet command-line filters. Use BenchmarkDotNet filters when developing or investigating a specific benchmark suite:

```bash
dotnet run --project Benchmarks/Benchmarks.csproj --configuration Release --no-build -- --filter "*RequestBenchmarks*"
dotnet run --project Benchmarks/Benchmarks.csproj --configuration Release --no-build -- --filter "*SqsBenchmarks*"
```

All normal BenchmarkDotNet command-line options remain available. BenchmarkDotNet owns benchmark discovery, filtering, exporters, jobs, diagnosers, and artifact generation.

## Release benchmark history

`.github/workflows/release-benchmarks.yml` runs the `ci` benchmark profile and requests BenchmarkDotNet's standard JSON exporter. The raw `BenchmarkDotNet.Artifacts` directory is retained as a GitHub Actions artifact for troubleshooting.

For real release runs, the workflow passes those JSON results to `martincostello/benchmarkdotnet-results-publisher`, which appends the summarized measurements to the repository's `benchmark-history` branch. This gives the project a durable release-to-release history without maintaining a custom result schema, metadata manifest, directory convention, or release ZIP format.

Manual executions of the benchmark workflow still produce the raw Actions artifact but do not publish into the historical data set.

The published data is intended for relative trend analysis. GitHub-hosted runners are not controlled benchmark hardware, so the history should not be treated as an absolute or regression-gating baseline. BenchmarkDotNet's own reports include runtime and environment information useful when interpreting individual runs.

## Reproducibility

Publishable performance measurements that require stable absolute comparisons should still be collected on controlled hardware. Keep the machine on external power where applicable and avoid material background workloads while collecting results.

The benchmark-validation GitHub Actions workflow verifies the pinned SDK, builds the benchmark solution, and exercises the `full`, `ci`, and `stress` profile selectors through BenchmarkDotNet list mode. It proves that benchmark code and profile selection remain valid without producing timed performance results.

## Controlled V5 to V6 reference

This is the controlled-hardware V5 to V6 reference collected on 2026-08-20. The exact measured repository commit is `a94a87c669969c3f2784f8d460b7c090a441573e`.

### Reference environment and method

- Dell XPS 16 9640 with an Intel Core Ultra 9 185H (16 physical cores, 22 logical cores) and 63.46 GiB RAM;
- Windows 11 Enterprise 24H2, build 26100.9106;
- external AC power and the High performance power plan;
- .NET SDK 10.0.400, .NET runtime 10.0.11 x64 RyuJIT, and BenchmarkDotNet 0.15.8.

The benchmark solution was built once with:

```bash
dotnet build Benchmarks.slnx --configuration Release --no-incremental -warnaserror
```

Each primary comparison was then run three times as independent BenchmarkDotNet processes using the `full` profile and a focused class filter: `Benchmarks.RequestBenchmarks.*`, `Benchmarks.SqsBenchmarks.*`, or `Benchmarks.AsyncSqsBenchmarks.*`. Every process used the JSON exporter. The published time is the median of the three process means; the allocation is the median bytes allocated per operation. Raw reports, logs, the run manifest, and the per-case aggregation are preserved outside the repository in `C:\Users\rg1844\Development\My\AWSLambdaSharpTemplate-benchmark-artifacts\2026-08-20-a94a87c`.

Controlled local hardware is the source for these absolute comparisons. The GitHub-hosted benchmark history remains useful as release-to-release trend and canary data, but it is not an absolute performance baseline.

Raw SDK is included as the lower-bound framework-cost reference. The primary migration comparison is V5 typed to V6 typed; Raw SDK parity is not a V6 performance requirement.

### Request baseline

The request benchmark is intentionally trivial, so it exposes invocation-framework overhead more strongly than a real application workload.

| Model | Median mean | Allocated | Time vs V5 | Allocation vs V5 | Time spread |
| --- | ---: | ---: | ---: | ---: | ---: |
| Raw SDK | 19.73 ns | 128 B | 0.16x | 0.36x | 12.14% |
| V5 | 123.18 ns | 352 B | 1.00x | 1.00x | 11.46% |
| V6 | 676.35 ns | 1,672 B | 5.49x | 4.75x | 3.71% |

The relative increase is large because the workload itself does almost no work, while the median absolute difference remains below one microsecond. This benchmark is best read as the cost floor of the richer V6 request pipeline rather than as a prediction of end-to-end application latency.

### SQS framework-overhead floor

The synchronous SQS suite uses already-completed tasks/value tasks. It is the framework-overhead floor, not a representative production completion mode.

| Batch | Model | Median mean | Allocated | Time vs V5 | Allocation vs V5 | Time spread |
| ---: | --- | ---: | ---: | ---: | ---: | ---: |
| 1 | Raw SDK | 0.210 us | 336 B | 0.44x | 0.75x | 2.97% |
| 1 | V5 typed | 0.474 us | 448 B | 1.00x | 1.00x | 8.90% |
| 1 | V6 raw | 1.808 us | 2,784 B | 3.81x | 6.21x | 4.31% |
| 1 | V6 typed | 2.095 us | 2,944 B | 4.42x | 6.57x | 3.66% |
| 10 | Raw SDK | 1.445 us | 1,560 B | 0.48x | 0.51x | 2.48% |
| 10 | V5 typed | 2.997 us | 3,040 B | 1.00x | 1.00x | 3.36% |
| 10 | V6 raw | 9.824 us | 12,432 B | 3.28x | 4.09x | 7.69% |
| 10 | V6 typed | 11.531 us | 14,032 B | 3.85x | 4.62x | 6.41% |
| 100 | Raw SDK | 14.185 us | 13,800 B | 0.52x | 0.48x | 13.36% |
| 100 | V5 typed | 27.286 us | 28,960 B | 1.00x | 1.00x | 6.60% |
| 100 | V6 raw | 82.356 us | 108,912 B | 3.02x | 3.76x | 6.50% |
| 100 | V6 typed | 99.051 us | 124,912 B | 3.63x | 4.31x | 8.74% |

This is deliberately a framework-overhead benchmark. V6 performs substantially more work per record than V5, including record-scoped infrastructure and source-specific processing semantics, so this suite should not be used in isolation to drive optimization decisions.

### Genuinely asynchronous SQS

The asynchronous SQS suite forces one real local suspension per record through `Task.Yield()`. It is still synthetic and does not model network or service latency, but it exercises the async control flow used by typical I/O-bound handlers.

| Batch | Model | Median mean | Allocated | Time vs V5 | Allocation vs V5 | Time spread |
| ---: | --- | ---: | ---: | ---: | ---: | ---: |
| 1 | Raw SDK | 1.038 us | 504 B | 0.70x | 0.53x | 9.14% |
| 1 | V5 typed | 1.484 us | 960 B | 1.00x | 1.00x | 1.09% |
| 1 | V6 raw | 3.104 us | 3,192 B | 2.09x | 3.33x | 5.75% |
| 1 | V6 typed | 3.512 us | 3,431 B | 2.37x | 3.57x | 2.52% |
| 10 | Raw SDK | 5.801 us | 1,730 B | 0.54x | 0.38x | 7.48% |
| 10 | V5 typed | 10.678 us | 4,497 B | 1.00x | 1.00x | 29.95% |
| 10 | V6 raw | 46.273 us | 15,423 B | 4.33x | 3.43x | 10.00% |
| 10 | V6 typed | 52.896 us | 17,821 B | 4.95x | 3.96x | 21.10% |
| 100 | Raw SDK | 57.945 us | 14,032 B | 0.70x | 0.35x | 7.75% |
| 100 | V5 typed | 82.655 us | 39,832 B | 1.00x | 1.00x | 0.84% |
| 100 | V6 raw | 227.757 us | 137,099 B | 2.76x | 3.44x | 10.37% |
| 100 | V6 typed | 294.355 us | 161,069 B | 3.56x | 4.04x | 13.33% |

Real suspension remains a more relevant guardrail for async-pipeline conclusions than the completed-task floor. The richer V6 record pipeline remains measurable in both time and allocations, but the batch-10 timing spread shows why these ratios should not be treated as universal throughput multipliers.

### Cross-run stability

Time spread is `(max - min) / median` across the three independent process means. Request spread was 3.71% to 12.14%, synchronous SQS spread was 2.48% to 13.36%, and genuinely asynchronous SQS spread was 0.84% to 29.95%. The 29.95% async batch-10 V5 result and the 21.10% V6 typed result make that particular timing ratio less stable than the allocation signal.

Allocations were exact across every Request and synchronous SQS run. Asynchronous SQS allocation differed by at most 13 B across runs (0.07% of the affected result). The preserved aggregation contains each individual process mean, median, min, max, spread, and allocation value for every published target and batch.

### Nested context

One refreshed batch-10 nested execution is included as context only, not as part of the repeated primary baseline. A nested workload gives a different view because V6 owns more of the useful event plumbing instead of comparing only framework dispatch around a trivial leaf handler.

| Model | Sync mean | Sync allocated | Async mean | Async allocated |
| --- | ---: | ---: | ---: | ---: |
| Raw SDK | 32.949 us | 38.95 KB | 62.110 us | 39.20 KB |
| V5 | 35.816 us | 40.39 KB | 80.425 us | 42.18 KB |
| V6 | 57.448 us | 61.12 KB | 193.772 us | 68.19 KB |

In this contextual run V6 is about 1.60x V5 synchronously and 2.41x V5 with a genuinely asynchronous leaf, while allocation is about 1.51x and 1.62x V5 respectively. The structural difference matters: V6 is performing source-specific S3 decoding/record processing and context propagation that remain application-owned in the Raw SDK and V5 implementations.

### Interpreting the V6 cost

V6 is a programming-model redesign rather than an optimization of the V5 execution path. The additional measured cost corresponds to capabilities that are intentionally part of the V6 model, including:

- one independent DI scope per record with deterministic disposal;
- source-specific immutable record contexts and access to raw/origin records;
- built-in record result and partial-batch response handling;
- automatic exception-to-source-result translation;
- cancellation and consistent async composition;
- record-level telemetry seams;
- reusable nested record processing and context propagation.

Those features do not make every additional allocation unavoidable, and the benchmark suite remains the regression safety net for future implementation improvements. The important distinction is that V5 and V6 are not doing the same amount of framework work. The migration tradeoff is therefore not only throughput versus throughput: V6 moves more AWS event-processing policy and lifecycle behavior from application code into the framework.

The result-list sizing change was the low-risk avoidable allocation identified in the separate performance-hardening work. DI activation, async composition, no-listener telemetry short-circuit, and typed-handler fast-path candidates were also investigated, but did not justify production changes before beta 4.

For performance-sensitive migrations, read synchronous and genuinely asynchronous results together, pay close attention to allocations, and validate the actual workload.

## Current coverage

### Request functions

The request benchmark uses a trivial uppercase workload to compare:

- a plain AWS Lambda handler (`RawSdk`), used as the BenchmarkDotNet baseline;
- the published v5 runtime (`V5`);
- the current v6 source tree (`V6`).

The workload itself is shared so the comparison focuses on invocation-framework overhead rather than different application implementations.

### SQS functions

The synchronous SQS benchmark measures batches of 1, 10, and 100 records and compares:

- a plain AWS Lambda SQS handler (`RawSdk`), used as the BenchmarkDotNet baseline;
- the published v5 typed SQS function (`V5Typed`);
- the current v6 raw SQS function (`V6Raw`);
- the current v6 typed SQS function (`V6Typed`).

Every target receives an equivalent pre-built SQS envelope. Envelope construction and target loading happen during benchmark setup so the measured operation focuses on dispatch, decoding, record handling, and response construction.

The synchronous handlers return already-completed tasks/value tasks. That makes this suite useful as a framework-overhead floor, but it should not be treated as representative of the completion mode of most production record handlers.

### Asynchronous SQS functions

`AsyncSqsBenchmarks` repeats the same Raw SDK, v5, v6 raw, and v6 typed comparison while forcing one real asynchronous suspension per record using the shared `AsyncWorkload.Suspend()` helper.

The helper returns `Task.Yield()` directly so the benchmark remains deterministic, local, and independent of network or service latency without adding a separate helper task state machine. It models the control-flow and allocation effects of a handler that actually suspends; it does not attempt to model DynamoDB, S3, HTTP, or other I/O latency.

The synchronous and asynchronous suites should be interpreted together. A change that improves only the already-completed path may be interesting as framework-floor data, but production optimization decisions should not be driven by synchronous-only wins without checking the genuinely asynchronous path as well.

### SQS partial-batch failures

The failure benchmarks use a fixed batch of 10 records and cover deterministic 0%, 10%, 50%, and 100% failure rates with no randomness during measurement. The smaller matrix keeps exception-heavy runs practical while still covering all-success, one-failure, mixed-failure, and all-failure behavior.

`SqsReturnedFailureBenchmarks` measures ordinary partial-batch failure results: the handler completes normally and identifies records that should be returned in `BatchItemFailures`. `SqsExceptionFailureBenchmarks` measures the distinct exception path, where per-record processing throws and the implementation translates the exception into the same logical partial-batch response. Both suites validate the expected failed-record count during BenchmarkDotNet setup before measurements begin.

The Raw SDK target contains the hand-written record loop and failure collection needed to implement those semantics directly. V5 is also included, but its published SQS module has no per-record partial-batch result contract. The V5 benchmark therefore uses `RequestResponseFunction<SQSEvent, SQSBatchResponse>` and implements JSON decoding, record iteration, per-record exception handling, and response construction in the consumer handler. This is intentionally the realistic V5 route to obtain the same AWS behavior rather than inventing a V6-style abstraction inside the old SQS module.

V6 measures both raw and typed record handlers and uses its built-in record-result and exception-translation pipeline. The comparison therefore captures not only runtime cost but also where the partial-batch plumbing lives in each programming model.

The V6 exception benchmark functions clear logging providers so repeated BenchmarkDotNet invocations do not flood stdout with one error message per failed record. Exception handling and translation still run through the normal framework path, but provider output cost is intentionally excluded from this suite.

The failure matrix remains synchronously completed to isolate the incremental cost of partial-batch handling and exception translation. The separate asynchronous SQS suite covers genuine suspension without multiplying every failure percentage by another completion-mode dimension.

### Nested SQS to SNS to S3

The nested-envelope benchmarks model an SQS batch where every message contains an SNS-style envelope whose `Message` contains one S3 event. Each S3 event contains one S3 object-created record.

The suites use batches of 1 and 10 SQS messages and compare:

- a Raw SDK implementation that owns SQS iteration, SNS-envelope decoding, S3-event decoding, and S3-record iteration directly;
- a V5 implementation that uses the V5 typed SQS message handler for the outer SNS envelope, while application code still owns S3-event decoding and S3-record iteration;
- a V6 implementation that follows the recommended nested flow: typed SQS handling for the SNS envelope, an S3 decoder, and the reusable S3 record processor/handler pipeline.

The Raw SDK and V5 implementations use case-insensitive `System.Text.Json` options when manually decoding the canonical AWS S3 event JSON, matching the lowercase AWS property names without changing the shared fixture.

`NestedSqsSnsS3Benchmarks` uses a synchronously completed S3 leaf handler. `NestedAsyncSqsSnsS3Benchmarks` uses the same deterministic `Task.Yield()` suspension at the S3 leaf so the nested pipeline is also measured when application work genuinely suspends.

The V6 leaf handler also reads the originating raw SQS message from the propagated S3 record context. This exercises one of the context-propagation capabilities provided by the V6 record model rather than treating the extra pipeline solely as dispatch overhead.

The runtime numbers should be read together with the structural difference between implementations. Raw SDK owns all envelope plumbing. V5 removes the outer SQS decoding but still leaves nested S3 parsing/iteration in application code. V6 supplies the source-specific S3 decoder/processor and context propagation, at the cost of the additional framework machinery those capabilities require.
