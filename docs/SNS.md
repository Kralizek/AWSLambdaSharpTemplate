# SNS

Use `Kralizek.Lambda.Template.Sns` for Lambda functions triggered by Amazon SNS.

## Typed notifications

```csharp
public sealed class Function
    : SnsFunction<OrderCreated, OrderCreatedHandler>;
```

Typed handlers implement `ISnsNotificationHandler<TNotification>`. The SNS `Message` is decoded through `IStringPayloadDecoder<TNotification>` and JSON decoding is registered by default.

Use `SnsFunction<THandler>` with `ISnsRecordHandler` when the handler needs the original `SNSEvent.SNSRecord`. Typed handlers can also retrieve it from `SnsNotificationContext.GetSnsRecord()`.

## Multiple records and failures

Handlers return `SnsRecordResult.Completed`. SNS events may contain multiple records and each record receives its own dependency-injection scope. Sequential processing is the default; `ParallelSnsFunction<...>` variants provide bounded parallel processing.

SNS does not support partial batch responses. If decoding or handling any record throws, the entire Lambda invocation fails so AWS can apply normal SNS/Lambda retry semantics.
