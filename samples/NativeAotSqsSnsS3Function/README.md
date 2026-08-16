# Native AOT SQS → SNS envelope → S3 sample

Use this sample when S3 notifications are published to SNS, delivered to SQS using the standard SNS JSON envelope, and consumed by a Native AOT Lambda. The SQS function decodes the outer SNS envelope as its typed payload, while the handler decodes the nested S3 event and delegates each S3 record to the framework's normal S3 processor.

```text
S3 bucket
  → SNS topic (standard delivery)
  → SQS queue
  → SQSEvent
  → SqsFunction<SnsEnvelope, SqsSnsS3Handler>
  → SqsSnsS3Handler
      → decode SnsEnvelope.Message to S3Event
      → IRecordProcessor<S3 record, S3RecordResult, RecordContext>
  → S3ObjectEventHandler
```

The application flow is the same as [SqsSnsS3Function](../SqsSnsS3Function/). Native AOT changes how JSON metadata and the Lambda runtime boundary are supplied, not how the topology handler or S3 item handler are structured.

## The three JSON boundaries

There are three distinct serialization steps in this topology:

1. Lambda Runtime API → `SQSEvent` and `SQSBatchResponse`.
2. `SQSMessage.Body` → `SnsEnvelope`.
3. `SnsEnvelope.Message` → `S3Event`.

`Program.cs` owns the Lambda Runtime API boundary through `LambdaJsonSerializerContext` and `SourceGeneratorLambdaJsonSerializer<TContext>`.

`PayloadJsonSerializerContext` owns the two application payload types:

```csharp
[JsonSerializable(typeof(SnsEnvelope))]
[JsonSerializable(typeof(S3Event))]
internal partial class PayloadJsonSerializerContext : JsonSerializerContext;
```

`Function.ConfigureFrameworkServices` registers the generated metadata used by the typed SQS decoder for the outer SNS envelope:

```csharp
protected override void ConfigureFrameworkServices(IServiceCollection services) =>
    services.AddSingleton(PayloadJsonSerializerContext.Default.SnsEnvelope);
```

The nested SNS `Message` decoder is registered explicitly with generated `JsonTypeInfo<S3Event>`:

```csharp
services.TryAddSingleton<IStringPayloadDecoder<S3Event>>(
    new JsonStringPayloadDecoder<S3Event>(PayloadJsonSerializerContext.Default.S3Event));
```

`SqsSnsS3Handler` therefore has the same shape as the non-AOT sample: it receives a typed `SnsEnvelope`, decodes only the nested `S3Event`, and delegates each S3 SDK record to `IRecordProcessor`.

`services.AddS3ObjectEventProcessing<S3ObjectEventHandler>()` registers the canonical S3 record-processing path. The processor preserves the S3 per-record DI scope, telemetry, adapter behavior, and `S3RecordContext` creation before invoking the ordinary `IS3ObjectEventHandler`.

## Publishing

Publish for the Lambda target runtime, for example:

```bash
dotnet publish samples/NativeAotSqsSnsS3Function -c Release -r linux-x64 --self-contained true --warnaserror
```

The included `aws-lambda-tools-defaults.json` contains matching Native AOT deployment defaults.

For the complete SNS/SQS/S3 infrastructure sketch and example payload, see [SqsSnsS3Function](../SqsSnsS3Function/). For the same topology with SNS Raw Message Delivery enabled, see [SqsRawSnsS3Function](../SqsRawSnsS3Function/); in that shape the typed SQS payload is `S3Event` directly.
