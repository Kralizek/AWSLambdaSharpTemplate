# Payload Decoding

Some AWS integrations carry an application payload inside a source-specific envelope. Those integrations use lightweight decoder abstractions from `Kralizek.Lambda.Template.Abstractions`.

Text payloads use `IStringPayloadDecoder<TPayload>` and binary payloads use `IBinaryPayloadDecoder<TPayload>`.

The runtime package provides System.Text.Json implementations for both representations and supports reflection-based `JsonSerializerOptions` as well as source-generated `JsonSerializerContext` / `JsonTypeInfo<T>` metadata.

```csharp
services.AddSingleton<IStringPayloadDecoder<OrderCreated>>(
    new JsonStringPayloadDecoder<OrderCreated>(AppJsonContext.Default.OrderCreated));
```

SQS and SNS use string decoders for message bodies. Kinesis Streams uses binary decoders for record data. EventBridge does not use a second decoder because `CloudWatchEvent<TDetail>.Detail` is deserialized directly by the Lambda serializer.

## Native AOT

Typed SQS, SNS, and Kinesis templates have a second JSON boundary inside the AWS event envelope. When generated with `--aot`, they replace only the default reflection-based payload decoder with one backed by a source-generated payload context. Handler registration remains part of the non-replaceable framework registration pipeline.

For example, typed SQS uses:

```csharp
services.AddSingleton<IStringPayloadDecoder<OrderCreated>>(
    new JsonStringPayloadDecoder<OrderCreated>(
        PayloadJsonSerializerContext.Default.OrderCreated));
```

The generated handler-side file owns that metadata:

```csharp
[JsonSerializable(typeof(OrderCreated))]
internal partial class PayloadJsonSerializerContext : JsonSerializerContext;
```

`Program.cs` owns a separate `LambdaJsonSerializerContext` for the outer AWS Lambda request/response boundary. Keeping the two contexts separate makes the ownership explicit and avoids coupling nested application payload metadata to Lambda bootstrap plumbing.

Kinesis uses the equivalent `JsonBinaryPayloadDecoder<T>` registration.

When application code replaces `OrderCreated` with its own contract, add that application type to the payload serializer context and update the decoder registration to use its generated `JsonTypeInfo<T>` property.

Raw SQS, SNS, and Kinesis variants do not deserialize an application payload, so `--aot --raw` does not require application payload metadata or a custom payload decoder registration.

Custom decoder packages can depend only on the abstractions package when they do not need the full runtime.
