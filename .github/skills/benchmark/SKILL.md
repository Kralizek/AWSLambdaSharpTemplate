---
name: benchmark
description: Build, run, and interpret the AWSLambdaSharpTemplate BenchmarkDotNet suite consistently, including local controlled measurements and benchmark profiles.
---

# Benchmarking AWSLambdaSharpTemplate

Use this skill when working on performance, benchmark infrastructure, benchmark results, or changes that need before/after measurements.

## Toolchain

The benchmark subtree is intentionally isolated from the main solution and pins its SDK through `benchmarks/global.json`.

Always run benchmark commands from `benchmarks/` and verify the pin first:

```bash
cd benchmarks
dotnet --version
```

Expected SDK: `10.0.400`.

Build before timed execution:

```bash
dotnet build Benchmarks.slnx --configuration Release --no-incremental -warnaserror
```

## Profiles

The BenchmarkDotNet host supports three profiles:

- `full`: default; complete matrix for local investigation and controlled-hardware measurements.
- `ci`: representative canary subset used on GitHub-hosted runners.
- `stress`: heavier batch sizes and failure-rate cases.

Run a profile directly:

```bash
dotnet run --project Benchmarks/Benchmarks.csproj --configuration Release --no-build -- --profile full
dotnet run --project Benchmarks/Benchmarks.csproj --configuration Release --no-build -- --profile ci
dotnet run --project Benchmarks/Benchmarks.csproj --configuration Release --no-build -- --profile stress
```

Profile execution is non-interactive. The host selects all benchmark cases allowed by the profile when no explicit BenchmarkDotNet `--filter` is supplied.

Use an explicit filter for focused investigation:

```bash
dotnet run --project Benchmarks/Benchmarks.csproj --configuration Release --no-build -- --profile full --filter "*SqsBenchmarks*"
```

Normal BenchmarkDotNet options such as exporters, artifacts, and list mode can be appended after `--`.

## Benchmark coverage

The suite currently covers:

- request/response
- synchronous SQS batches 1, 10, and 100
- genuinely asynchronous SQS batches 1, 10, and 100 using deterministic `Task.Yield()` suspension
- returned SQS partial-batch failures at 0%, 10%, 50%, and 100%
- exception-to-partial-batch failures at 0%, 10%, 50%, and 100%
- nested SQS -> SNS -> S3 processing, synchronous and genuinely asynchronous

The comparison targets include Raw AWS SDK, published V5, and current V6 raw/typed shapes where applicable.

## Interpretation rules

Do not optimize for a single microbenchmark in isolation.

In particular:

1. Treat synchronous SQS as the framework-overhead floor, not a representative production completion mode.
2. Check genuinely asynchronous SQS before recommending async-control-flow changes.
3. Use allocation results alongside CPU time; allocations are often the more stable signal of framework cost.
4. Keep returned failures separate from exception-driven failures. Exception translation is intentionally more expensive.
5. Read nested benchmarks together with their programming-model differences: V6 owns more envelope decoding, record processing, context propagation, and failure behavior than Raw/V5 consumer code.
6. Do not treat Raw SDK parity as the goal for normal V6. Raw is the lower-bound framework-cost reference.
7. Preserve V6 semantics when evaluating optimizations: one DI scope per record, immutable/source-specific contexts, raw-origin access, partial-batch behavior, cancellation, exception translation, deterministic disposal ordering, sequential processing, and observability seams.
8. A synchronous record fast path was previously tried and reverted because realistic async benefit did not justify the complexity. Do not resurrect it without new evidence across genuine async workloads.

## Controlled measurements

Use local fixed hardware for publishable absolute comparisons. GitHub-hosted runners are useful for trend canaries, not absolute performance baselines.

For controlled runs:

- start from a clean checkout and record the exact commit SHA;
- keep the machine on external power where applicable;
- avoid material background work such as builds, containers, indexing, backups, or heavy browser activity;
- record CPU, RAM, OS, SDK/runtime, BenchmarkDotNet version, and collection date;
- preserve raw `BenchmarkDotNet.Artifacts`;
- use independent BenchmarkDotNet executions when assessing repeatability rather than relying only on iterations inside one execution.

When documentation requires a stable reference baseline, run the relevant comparisons at least three independent times and report cross-run spread before treating the figures as final.

## Performance investigations

When changing production code for performance:

1. Establish the baseline first.
2. State the hypothesis and which cost is being targeted.
3. Keep production semantics unchanged unless the task explicitly proposes a programming-model change.
4. Measure the smallest useful before/after comparison.
5. Re-run the broader relevant profile before concluding that the change is worthwhile.
6. Report both absolute values and meaningful relative comparisons, especially V5 -> V6 for migration-facing work.
7. Separate measured facts from architectural interpretation.

Benchmark-only prototypes are acceptable for testing architectural hypotheses. Keep them out of production code until measurements and API/semantic review justify promotion.
