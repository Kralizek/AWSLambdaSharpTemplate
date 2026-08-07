# Kralizek.Lambda.Template.Abstractions

`Kralizek.Lambda.Template.Abstractions` contains the consumer-facing contracts shared by the Lambda programming model and source-specific integrations.

The package intentionally has no dependency on AWS Lambda runtime types, dependency injection, configuration, logging, or JSON serialization. Libraries can implement handlers, contexts, and payload decoders without loading the full runtime package.

## Handler contracts

The package defines the handler contracts used by the semantic function roots:

- `IEventHandler<TInput>` and `IEventHandler<TInput, TContext>`
- `IRequestHandler<TInput, TOutput>` and `IRequestHandler<TInput, TOutput, TContext>`
- `IRecordHandler<TRecord, TRecordResult, TContext>`

The compact forms use the standard `EventContext` and `RequestContext`. Full forms allow source-specific integrations to expose richer context types.

## Function contexts

`FunctionContext` exposes common invocation metadata through strongly typed, source-neutral properties:

- `AwsRequestId`
- `FunctionName`
- `FunctionVersion`
- `InvokedFunctionArn`
- `MemoryLimitInMB`
- `RemainingTime`
- `LogGroupName`
- `LogStreamName`

The standard semantic contexts are `EventContext`, `RequestContext`, and `RecordContext`.

Contexts also expose a `Properties` bag for runtime-specific data that is not represented by the strongly typed contract. The abstractions package does not interpret those values or depend on the runtime types stored in the bag.

`Kralizek.Lambda.Template` maps the AWS `ILambdaContext` into the strongly typed metadata and preserves the original runtime context in this bag. Applications that reference the main package can retrieve it through `GetLambdaContext()` when they need information not surfaced by the abstractions.

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

## Lightweight extension packages

A custom handler, context, or decoder library can reference only this package:

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