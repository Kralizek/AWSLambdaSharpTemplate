# Kralizek.Lambda.Template.Sqs

SQS specialization for `Kralizek.Lambda.Template`.

```csharp
public sealed class Function
    : SqsFunction<OrderCreated, OrderCreatedHandler>;
```

Handlers receive the decoded message together with an `SqsMessageContext` exposing the raw SQS record and the common invocation metadata.

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

Messages are decoded through `IStringPayloadDecoder<TMessage>`. JSON is the default:

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

`SqsFunction<TMessage,THandler>` processes records sequentially. `ParallelSqsFunction<TMessage,THandler>` uses bounded parallel processing. Both create an invocation scope plus an independent nested scope for each SQS record.

Handler or decoder failures are returned through `SQSBatchResponse.batchItemFailures`. Invocation cancellation is not converted into a partial failure; it aborts the invocation so AWS can retry the batch.
