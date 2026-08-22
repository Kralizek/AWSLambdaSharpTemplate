# Minimal hosting

Version 6 has one request/event handler programming model and two hosting choices.

The normal `EventFunction` and `RequestFunction` hosts provide the complete v6 Request/Event invocation infrastructure. `MinimalEventFunction` and `MinimalRequestFunction` host the same handlers through a deliberately shorter execution path when the function does not need all of that host-level infrastructure.

Minimal hosting is a hosting choice, not a second application programming model. For supported request/event functions, the handler contract and application code stay the same:

```csharp
public class Function : RequestFunction<string, string, UpperCaseHandler>;
```

can become:

```csharp
public class Function : MinimalRequestFunction<string, string, UpperCaseHandler>;
```

without changing `UpperCaseHandler`.

## What minimal hosting keeps

Minimal hosting deliberately spends a small amount of runtime overhead on application structure that remains useful even for simple Lambda functions:

- function-local configuration;
- function-local dependency injection;
- Lambda-compatible logging and application logging configuration;
- one dependency-injection scope per invocation;
- handler activation through the invocation scope;
- the same `EventContext` / `RequestContext` contracts as the normal host;
- the original `ILambdaContext` escape hatch through the framework context;
- cancellation tied to the Lambda invocation's remaining time when enabled;
- asynchronous disposal of scoped services.

This is intentionally similar to the useful infrastructure that v5 could provide at comparatively low invocation cost, while retaining the v6 handler and context contracts.

## What minimal hosting omits

For Request/Event functions, Minimal does not execute the default/full host's internal invocation telemetry path. In particular, Minimal does not emit Kralizek.Lambda.Template `ActivitySource` activities or `Meter` instruments for the invocation.

Separately, Minimal hosting in v6.0 does not extend to source-specific Record hosts. Therefore it also does not provide the source-specific Record-processing capabilities owned by those hosts, such as:

- per-record processing and source-specific result semantics;
- source-specific envelope orchestration;
- partial-batch response translation;
- nested record-context propagation;
- source-specific record processors.

Those Record capabilities are not being removed from `EventFunction` or `RequestFunction`; they belong to the source-specific Record model and remain available through the normal source-specific hosts.

There are intentionally no `MinimalSqsFunction`, `MinimalSnsFunction`, `MinimalS3Function`, or equivalent source-specific Minimal types in v6.0.

## Performance

Minimal and full V6 hosting are two current hosting choices with different capability and performance profiles.

The current release Request benchmark measured:

| Model | Mean | Allocated |
| --- | ---: | ---: |
| Raw AWS SDK | 32.2 ns | 128 B |
| V5 | 196.7 ns | 352 B |
| V6 Minimal | 299.6 ns | 584 B |
| V6 full | 443.0 ns | 712 B |

The workload is intentionally trivial, so these numbers expose framework cost rather than realistic end-to-end Lambda latency. In this snapshot, Minimal was about 32% faster than the full V6 Request host and allocated 128 B less per invocation. Compared with V5 it retained a measurable framework floor of roughly 103 ns and 232 B.

A nested SQS -> SNS -> S3 benchmark gives a more application-shaped comparison:

| Model | Mean | Allocated |
| --- | ---: | ---: |
| Raw AWS SDK | 5.60 us | 4,168 B |
| V5 | 6.01 us | 4,280 B |
| V6 Minimal comparison | 5.88 us | 4,624 B |
| V6 full/source-specific | 7.93 us | 6,088 B |

The `V6Minimal` contender in source-specific benchmark suites is deliberately application-owned orchestration running on Minimal hosting. It is not a `MinimalSqsFunction` or other source-specific Minimal API. The comparison shows the cost boundary between lean V6 hosting and the source-specific processing that the full Record model owns.

The important conclusion is not that Minimal makes V6 equivalent to V5 in every microbenchmark. It is that V6 exposes an explicit capability/performance choice while keeping the same Request/Event handler programming model. Minimal substantially lowers the framework floor; the full host intentionally spends more on framework-owned behavior.

GitHub-hosted release measurements are useful for trends, but they are not controlled hardware. Consumers with strict latency or allocation requirements should benchmark their own workload. See [the benchmark documentation](../benchmarks/README.md) for the broader matrix and methodology.

## OpenTelemetry

`--minimal --otel` still uses the standard AWS Lambda OpenTelemetry wrapper around `FunctionHandlerAsync`, so the Lambda invocation is traced.

The generated Minimal function does **not** subscribe to `LambdaTelemetry.ActivitySourceName`, create a KLT `MeterProvider`, or flush KLT meters because Minimal does not emit those inner framework signals. Application code and dependencies can still register and emit their own OpenTelemetry activities and metrics normally.

Conceptually:

```text
Minimal + OpenTelemetry
└── Lambda invocation span
    └── application/dependency instrumentation, if configured

Normal + OpenTelemetry
└── Lambda invocation span
    ├── KLT internal activities and metrics
    └── application/dependency instrumentation
```

## Native AOT

Minimal hosting is compatible with the generic Request/Event templates' `--aot` option. AOT still controls executable hosting and source-generated Lambda serialization; Minimal controls the framework hosting path used once the invocation reaches the function.

```bash
dotnet new lambda-template-request --minimal --aot
```

## Templates

The generic Request and Event templates expose `--minimal`:

```bash
dotnet new lambda-template-request --minimal
dotnet new lambda-template-event --minimal
dotnet new lambda-template-event --minimal --otel
dotnet new lambda-template-request --minimal --aot
```

For these templates, switching between the normal and Minimal host changes `Function.cs`; handler and application files are unchanged.

`--raw` remains an independent payload/record-shape option on the source-specific record templates that support it. V6.0 intentionally does not add Minimal source-specific record hosts, so no generated source-specific template currently combines `--minimal` and `--raw`.

## Choosing the host

Prefer the normal Request/Event host when the function wants the full host's invocation infrastructure, including KLT internal invocation telemetry. Prefer the normal source-specific Record hosts when the function needs record processing, result/failure semantics, envelope orchestration, context propagation, or other source-specific behavior.

Prefer Minimal for source-neutral request/event functions where the application wants the v6 handler/context/DI model but does not need that additional host-level behavior and wants lower invocation overhead.

The normal host remains the default. Minimal is an explicit performance-oriented trade-off, not a compatibility mode.
