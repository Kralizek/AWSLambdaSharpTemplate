# Native AOT SQS → SNS envelope → S3 sample

Use this sample when S3 notifications are published to SNS, delivered to SQS using the standard SNS JSON envelope, and consumed by a Native AOT Lambda. The raw SQS handler owns the nested envelope decoding while S3 record handling continues through the framework's normal S3 processor.

```text
S3 bucket
  → SNS topic (standard delivery)
  → SQS queue
  → SQSEvent
  → SqsFunction<SqsSnsS3Handler>
  → SqsSnsS3Handler
      → decode SQSMessage.Body to SnsEnvelope
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

`PayloadJsonSerializerContext` owns the two nested application payloads:

```csharp
[JsonSerializable(typeof(SnsEnvelope))]
[JsonSerializable(typeof(S3Event))]
internal partial class PayloadJsonSerializerContext : JsonSerializerContext;
```

The nested decoders are registered explicitly with their generated `JsonTypeInfo<T>` instances:

```csharp
services.TryAddSingleton<IStringPayloadDecoder<SnsEnvelope>>(
    new JsonStringPayloadDecoder<SnsEnvelope>(PayloadJsonSerializerContext.Default.SnsEnvelope));

services.TryAddSingleton<IStringPayloadDecoder<S3Event>>(
    new JsonStringPayloadDecoder<S3Event>(PayloadJsonSerializerContext.Default.S3Event));
```

This keeps both nested JSON boundaries AOT-safe without changing `SqsSnsS3Handler`. The handler receives the same decoder abstractions as the non-AOT sample, decodes both envelope levels, and delegates each S3 SDK record to `IRecordProcessor`.

`services.AddS3ObjectEventProcessing<S3ObjectEventHandler>()` registers the canonical S3 record-processing path. The processor preserves the S3 per-record DI scope, telemetry, adapter behavior, and `S3RecordContext` creation before invoking the ordinary `IS3ObjectEventHandler`.

## Publishing

Publish for the Lambda target runtime, for example:

```bash
dotnet publish samples/NativeAotSqsSnsS3Function -c Release -r linux-x64 --self-contained true --warnaserror
```

The included `aws-lambda-tools-defaults.json` contains matching Native AOT deployment defaults.

For the complete SNS/SQS/S3 infrastructure sketch and example payload, see [SqsSnsS3Function](../SqsSnsS3Function/). For the same topology with SNS Raw Message Delivery enabled, see [SqsRawSnsS3Function](../SqsRawSnsS3Function/); because its SQS body is already an `S3Event`, the typed SQS programming model remains the natural fit there.
