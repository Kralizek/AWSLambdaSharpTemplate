# Kralizek.Lambda.Template.Sqs

SQS specialization for `Kralizek.Lambda.Template`.

## Decoded messages

Use `SqsFunction<TMessage,THandler>` when the SQS body represents an application contract:

```csharp
public sealed class Function
    : SqsFunction<OrderCreated, OrderCreatedHandler>;
```

Handlers receive the decoded message together with an `SqsMessageContext` exposing SQS metadata and the common Lambda invocation metadata.

```csharp
public sealed class OrderCreatedHandler : ISqsMessageHandler<OrderCreated>
{
    public ValueTask HandleAsync(
        OrderCreated message,
        SqsMessageContext context,
        CancellationToken cancellationToken)
    {
        // process message
        return ValueTask.CompletedTask;
    }
}
```

Messages are decoded through `IStringPayloadDecoder<TMessage>`. JSON using the System.Text.Json web defaults is registered automatically:

```text
SQSEvent.SQSMessage.Body
        ↓
IStringPayloadDecoder<TMessage>
        ↓
TMessage
```

To select another decoder, register it from `ConfigureServices`:

```csharp
protected override void ConfigureServices(
    IServiceCollection services,
    IConfiguration configuration)
{
    services.AddSingleton<IStringPayloadDecoder<OrderCreated>>(
        new JsonStringPayloadDecoder<OrderCreated>(AppJsonContext.Default.OrderCreated));
}
```

Use `PlainTextStringPayloadDecoder` explicitly when an SQS body should be treated as raw text.

## Raw SQS records

Use `SqsFunction<THandler>` when the handler needs the original AWS SQS record and no body decoding should occur:

```csharp
public sealed class Function
    : SqsFunction<OrderRecordHandler>;

public sealed class OrderRecordHandler : ISqsRecordHandler
{
    public ValueTask HandleAsync(
        SQSEvent.SQSMessage record,
        SqsMessageContext context,
        CancellationToken cancellationToken)
    {
        // process the raw SQS record
        return ValueTask.CompletedTask;
    }
}
```

The raw model does not register or invoke an `IStringPayloadDecoder<TMessage>`.

## SQS context

`SqsMessageContext` exposes common SQS metadata such as the message ID, receipt handle, system attributes, message attributes, event source ARN, and AWS region.

The original AWS SDK record remains available as an explicit escape hatch:

```csharp
var sqsMessage = context.GetSqsMessage();
```

Likewise, the original Lambda runtime context can be retrieved with `context.GetLambdaContext()`.

## Processing and failures

`SqsFunction<THandler>` and `SqsFunction<TMessage,THandler>` process records sequentially. Their `ParallelSqsFunction` counterparts use bounded parallel processing. All variants create an invocation scope plus an independent nested scope for each SQS record.

Handler or decoder failures are returned through `SQSBatchResponse.batchItemFailures`. Invocation cancellation is not converted into a partial failure; it aborts the invocation so AWS can retry the batch.