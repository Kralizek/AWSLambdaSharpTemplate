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
