# OpenTelemetry

AWS Lambda Sharp Template emits framework diagnostics through the .NET `System.Diagnostics` APIs. The runtime libraries do not depend on the OpenTelemetry SDK: applications decide whether and how those diagnostics are collected and exported.

## Instrumentation identity

The core package exposes one shared instrumentation identity through `LambdaTelemetry`:

```csharp
LambdaTelemetry.ActivitySourceName
LambdaTelemetry.MeterName
```

Both currently resolve to `Kralizek.Lambda.Template`. Register these names with the tracing and metrics providers when collecting framework telemetry.

## Invocation spans

The framework does not create a second Lambda invocation span. When a function is wrapped with `OpenTelemetry.Instrumentation.AWSLambda`, the standard `AWSLambdaWrapper` creates the server span and the framework enriches the current activity with framework-specific information.

The invocation activity receives:

- `kralizek.lambda.function.model`: `request`, `event`, or `record`

Glue packages that represent one event or request per invocation enrich this same span with source-owned metadata. EventBridge adds event identity/source/type information, while Cognito adds trigger, user-pool, user, and region information. They do not create artificial record spans.

This keeps AWS Lambda/FaaS attributes and framework attributes on the same invocation span.

## Record spans

`RecordFunction` processing creates a child activity for each record through the shared activity source:

```text
AWS Lambda invocation
├─ record.process
├─ record.process
└─ record.process
```

Nested processing through `IRecordProcessor` naturally creates deeper child activities because each processor runs under the current record activity.

Each record-oriented AWS source package enriches the record activity with transport- or event-specific metadata that it owns. Examples include SQS and SNS message identifiers, Kinesis sequence and partition information, DynamoDB Streams event metadata, and S3 bucket/object identifiers. Where OpenTelemetry defines a matching semantic convention, the source package uses that attribute name. Framework-specific attributes use the `kralizek.aws.*` namespace instead of claiming a standard `aws.*` attribute name.

High-cardinality record identifiers belong on spans only. Framework metrics intentionally do not copy message IDs, object keys, sequence numbers, partition keys, resource ARNs, user names, or similar values into metric tags.

Business-specific telemetry is application-owned. Handlers should create their own activities and meters for domain concepts rather than extending framework source metadata with business identifiers.

Record activities are marked as errors when record processing throws or is canceled.

## Metrics

The shared meter currently emits:

| Instrument | Type | Unit | Description |
| --- | --- | --- | --- |
| `kralizek.lambda.invocations` | Counter | `{invocation}` | Framework invocations, tagged by function model |
| `kralizek.lambda.records` | Counter | `{record}` | Processed records, tagged by outcome |
| `kralizek.lambda.record.duration` | Histogram | `s` | Per-record processing duration, tagged by outcome |

Record outcomes are `success`, `error`, or `canceled`.

## Generate a function with OpenTelemetry enabled

Every project template accepts the `--otel` option:

```bash
dotnet new lambda-template-sqs --otel
```

OpenTelemetry is a template-time choice. When `--otel` is omitted, the template engine removes the OpenTelemetry code and package references entirely. The generated project contains no OpenTelemetry-specific source, package references, build properties, or helper files.

When `--otel` is enabled, the generated project:

- references `OpenTelemetry.Instrumentation.AWSLambda` and the OTLP exporter;
- overrides the inherited `FunctionHandlerAsync` and wraps the base implementation with `AWSLambdaWrapper.TraceAsync`;
- registers the framework activity source and meter;
- exposes `ConfigureTracing()` and `ConfigureMetrics()` methods directly in `Function.cs` so the OpenTelemetry setup stays local and easy to customize;
- force-flushes metrics at the end of the invocation so buffered measurements are not left behind when Lambda freezes the execution environment.

The OTLP exporter keeps the generated function portable across local Aspire development, ADOT, and other OpenTelemetry collectors or backends without changing the framework instrumentation model.

AWS X-Ray context extraction remains enabled by default so generated functions can participate in X-Ray propagation. The generated tracing configuration includes a commented `DisableAwsXRayContextExtraction = true` setting. Enable it when X-Ray is not being used and the Lambda-provided X-Ray context prevents OpenTelemetry spans from being recorded.
