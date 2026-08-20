# Minimal hosting

Version 6 has one request/event handler programming model and two hosting choices.

The normal `EventFunction` and `RequestFunction` hosts provide the complete v6 invocation infrastructure. `MinimalEventFunction` and `MinimalRequestFunction` host the same handlers through a deliberately shorter execution path when the function does not need the richer processing pipeline.

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
- cancellation tied to the Lambda invocation's remaining time;
- asynchronous disposal of scoped services.

This is intentionally similar to the useful infrastructure that v5 could provide at comparatively low invocation cost, while retaining the v6 handler and context contracts.

## What minimal hosting omits

The minimal host does not execute the normal host's internal invocation telemetry or richer processing pipeline. In particular it does not provide:

- Kralizek.Lambda.Template `ActivitySource` activities or `Meter` instruments;
- record processing or per-record result semantics;
- source-specific envelope orchestration;
- partial-batch response translation;
- nested record-context propagation;
- source-specific record processors.

Those capabilities remain part of the normal/source-specific v6 hosts.

There are intentionally no `MinimalSqsFunction`, `MinimalSnsFunction`, `MinimalS3Function`, or equivalent source-specific Minimal types in v6.0.

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

Prefer the normal host when the function uses source-specific processing semantics, record processing, framework telemetry, or other behavior provided by the full v6 pipeline.

Prefer Minimal for source-neutral request/event functions where the application wants the v6 handler/context/DI model but does not need that additional orchestration and wants invocation overhead closer to the raw Lambda runtime.

The normal host remains the default. Minimal is an explicit performance-oriented trade-off, not a compatibility mode.
