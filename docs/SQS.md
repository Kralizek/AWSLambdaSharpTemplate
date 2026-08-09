# SQS

Use `Kralizek.Lambda.Template.Sqs` for Lambda functions triggered by Amazon SQS.

## Typed messages

```csharp
public sealed class Function
    : SqsFunction<OrderCreated, OrderCreatedHandler>;
```

Handlers implement `ISqsMessageHandler<TMessage>`. The message body is decoded through `IStringPayloadDecoder<TMessage>`; JSON with System.Text.Json web defaults is registered automatically.

Use `SqsFunction<THandler>` with `ISqsRecordHandler` when the handler needs the original `SQSEvent.SQSMessage` and no body decoding should occur. Typed handlers can still access the original message through `SqsMessageContext.GetSqsMessage()`.

## Results and processing

Handlers return `SqsRecordResult.Success` or `SqsRecordResult.Failed(reason)`. Failed results and decoder/handler failures are represented through `SQSBatchResponse.batchItemFailures`; configure the event source mapping to honor partial-batch responses.

Sequential processing is the default. `ParallelSqsFunction<...>` variants provide bounded parallelism. Cancellation aborts the invocation rather than being converted into a record failure.
