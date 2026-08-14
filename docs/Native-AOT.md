# Native AOT

The project templates support Native AOT as a template-time hosting option. Pass `--aot` when creating a function to generate an executable Lambda host that uses AWS Lambda RuntimeSupport and source-generated System.Text.Json metadata.

```bash
dotnet new lambda-template-sqs --aot
```

Native AOT does not introduce another Kralizek runtime package or programming model. The generated `Function` still derives from the same request, event, or record-function specialization; `--aot` changes how Lambda starts the application and how the Lambda boundary is serialized.

## Generated hosting model

A regular generated project is a managed Lambda library: AWS invokes its `Assembly::Type::FunctionHandlerAsync` handler and the assembly-level `LambdaSerializer` attribute selects the serializer. An AOT project instead:

- builds as an executable with `PublishAot` enabled;
- starts the Lambda runtime loop with `LambdaBootstrapBuilder`;
- passes `FunctionHandlerAsync` to that bootstrap;
- uses `SourceGeneratorLambdaJsonSerializer<TContext>` for the Lambda boundary;
- deploys the executable assembly name as the handler;
- has no `LambdaSerializer` assembly attribute;
- publishes self-contained for Linux x64.

The generated `Program.cs` keeps these hosting details separate from `Function.cs` so application customization remains focused on the function and handler.

## Serialization metadata

Native AOT requires serialization metadata to be known at build time. The generated host owns a source-generated context for the Lambda boundary types implied by the selected template.

For typed SQS, SNS, and Kinesis templates there is a second, application-owned serialization context beside the generated handler. That context contains the nested payload type and contributes its generated `JsonTypeInfo<T>` to the framework's payload decoder.

For example, typed SQS effectively has two contexts:

```csharp
// Program.cs
[JsonSerializable(typeof(SQSEvent))]
[JsonSerializable(typeof(SQSBatchResponse))]
internal partial class LambdaJsonSerializerContext : JsonSerializerContext;
```

```csharp
// OrderCreatedHandler.cs
[JsonSerializable(typeof(OrderCreated))]
internal partial class PayloadJsonSerializerContext : JsonSerializerContext;
```

When replacing `OrderCreated` with an application contract, update the payload context accordingly.

A useful ownership rule is: **the generated host owns Lambda boundary metadata; application code owns nested application payload metadata.**

## Nested payload decoding

SQS, SNS, and Kinesis Streams have two serialization boundaries in decoded mode:

1. AWS Lambda deserializes the outer event envelope.
2. The framework decodes the record payload into the application type.

Typed generated templates register `PayloadJsonSerializerContext.Default.<Payload>` through `ConfigureFrameworkServices`. The framework decoder uses that `JsonTypeInfo<T>` for nested payload decoding. In normal JIT applications, reflection remains an available fallback when `JsonSerializer.IsReflectionEnabledByDefault` is true; Native AOT removes that fallback, so the generated metadata is required.

Raw record mode does not need application payload metadata because no nested payload decoding occurs.

## Combining `--aot`, `--raw`, and `--otel`

The template options are orthogonal:

| Option | Changes |
| --- | --- |
| `--raw` | Which record contract and handler shape are generated |
| `--aot` | Lambda hosting, publish mode, and serialization metadata |
| `--otel` | Wraps function execution with standard AWS Lambda OpenTelemetry instrumentation |

This allows combinations such as:

```bash
dotnet new lambda-template-sns --aot --raw
dotnet new lambda-template-event --aot --otel
dotnet new lambda-template-kinesis-stream --aot --otel --raw
```

OpenTelemetry remains an execution wrapper around the same inherited handler. Native AOT changes how that handler is hosted, not the framework lifecycle underneath it.

## Deployment

AOT-generated projects keep the AWS managed .NET 10 Lambda runtime, but `aws-lambda-tools-defaults.json` changes the handler from a managed `Assembly::Type::Method` entry to the executable assembly name and supplies the self-contained publish parameter expected by AWS Lambda tooling.

Native AOT publishing is platform-sensitive. Publish for `linux-x64` with `--self-contained true` on a Linux environment compatible with the Lambda target, or use the AWS tooling/container workflow appropriate for your build environment.

## Sample

See `samples/NativeAotSqsFunction` for a typed SQS example showing the executable bootstrap, generated boundary metadata, separate payload metadata, and an AOT-safe nested JSON decoder.
