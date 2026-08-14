# Native AOT SQS → SNS envelope → S3 sample

Use this sample when S3 notifications are published to SNS, delivered to SQS using the standard SNS JSON envelope, and consumed by a Native AOT Lambda.

```text
S3 bucket
  → SNS topic (standard delivery)
  → SQS queue
  → SQSEvent
  → SqsFunction<SnsEnvelope, SnsEnvelopedS3DeliveryHandler>
  → decode SnsEnvelope.Message to S3Event
  → S3EventDispatcher
  → IRecordProcessor<S3 record, S3RecordResult, RecordContext>
  → S3ObjectEventHandler
```

The application flow is the same as [SqsSnsS3Function](../SqsSnsS3Function/). The difference is that this project is an executable Native AOT Lambda and every JSON boundary uses source-generated metadata.

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

`Function.ConfigureFrameworkServices` registers the generated metadata used by the outer SQS payload decoder:

```csharp
protected override void ConfigureFrameworkServices(IServiceCollection services)
{
    services.AddSingleton(PayloadJsonSerializerContext.Default.SnsEnvelope);
}
```

The inner SNS `Message` decoding is registered explicitly with its generated `JsonTypeInfo<S3Event>` so DI cannot select a reflection-based `JsonStringPayloadDecoder<S3Event>` constructor:

```csharp
services.TryAddSingleton<IStringPayloadDecoder<S3Event>>(
    new JsonStringPayloadDecoder<S3Event>(PayloadJsonSerializerContext.Default.S3Event));
```

The handler and record-processing contracts remain unchanged. `SnsEnvelopedS3DeliveryHandler`, `S3EventDispatcher`, and `S3ObjectEventHandler` use the same framework contracts as the non-AOT sample.

## Publishing

Publish for the Lambda target runtime, for example:

```bash
dotnet publish samples/NativeAotSqsSnsS3Function -c Release -r linux-x64 --self-contained true --warnaserror
```

The included `aws-lambda-tools-defaults.json` contains matching Native AOT deployment defaults.

For the complete SNS/SQS/S3 infrastructure sketch and an example payload, see [SqsSnsS3Function](../SqsSnsS3Function/).
