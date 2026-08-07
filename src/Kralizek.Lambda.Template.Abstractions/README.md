# Kralizek.Lambda.Template.Abstractions

`Kralizek.Lambda.Template.Abstractions` contains lightweight contracts that can be implemented without referencing the full Lambda runtime package.

The package intentionally has no dependency on AWS Lambda runtime types, dependency injection, configuration, logging, or JSON serialization.

## Payload decoders

Payload decoders transform the raw payload extracted by an event-source integration into the application contract handled by your function.

For text-based payloads such as SQS message bodies and SNS messages, implement:

```csharp
public interface IStringPayloadDecoder<TPayload>
{
    ValueTask<TPayload> DecodeAsync(
        string payload,
        CancellationToken cancellationToken = default);
}
```

For event sources that expose binary payloads, implement:

```csharp
public interface IBinaryPayloadDecoder<TPayload>
{
    ValueTask<TPayload> DecodeAsync(
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken = default);
}
```

A decoder is deliberately unaware of SQS, SNS, Kinesis, or any other event source. Glue packages decide which raw representation they consume and depend only on the matching abstraction.

## Custom decoder packages

A custom decoder can reference only this package:

```xml
<PackageReference Include="Kralizek.Lambda.Template.Abstractions" Version="..." />
```

For example:

```csharp
public sealed class MyDecoder : IStringPayloadDecoder<OrderCreated>
{
    public ValueTask<OrderCreated> DecodeAsync(
        string payload,
        CancellationToken cancellationToken = default)
    {
        // Decode using the serialization format required by the application.
        throw new NotImplementedException();
    }
}
```

`Kralizek.Lambda.Template` provides ready-to-use System.Text.Json implementations for both string and binary payloads.