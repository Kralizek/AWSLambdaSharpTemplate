# Payload Decoding

Some AWS integrations carry an application payload inside a source-specific envelope. Those integrations use lightweight decoder abstractions from `Kralizek.Lambda.Template.Abstractions`.

Text payloads use `IStringPayloadDecoder<TPayload>` and binary payloads use `IBinaryPayloadDecoder<TPayload>`.

The runtime package provides System.Text.Json implementations for both representations and supports reflection-based `JsonSerializerOptions` as well as source-generated `JsonSerializerContext` / `JsonTypeInfo<T>` metadata.

```csharp
services.AddSingleton<IStringPayloadDecoder<OrderCreated>>(
    new JsonStringPayloadDecoder<OrderCreated>(AppJsonContext.Default.OrderCreated));
```

SQS and SNS use string decoders for message bodies. Kinesis Streams uses binary decoders for record data. EventBridge does not use a second decoder because `CloudWatchEvent<TDetail>.Detail` is deserialized directly by the Lambda serializer.

Custom decoder packages can depend only on the abstractions package when they do not need the full runtime.
