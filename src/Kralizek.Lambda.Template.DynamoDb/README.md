# Kralizek.Lambda.Template.DynamoDb

Amazon DynamoDB Streams integration for `Kralizek.Lambda.Template`.

Use `DynamoDbStreamFunction<THandler>` to process stream records sequentially, with one dependency-injection scope per record. `ParallelDynamoDbStreamFunction<THandler>` is available as an explicit bounded-parallel alternative when your processing model can tolerate records completing out of order.

```csharp
public sealed class Function
    : DynamoDbStreamFunction<OrderChangeHandler>;
```

Handlers receive the original AWS `DynamoDBEvent.DynamodbStreamRecord` together with `DynamoDbStreamRecordContext`:

```csharp
public sealed class OrderChangeHandler : IDynamoDbStreamRecordHandler
{
    public ValueTask HandleAsync(
        DynamoDBEvent.DynamodbStreamRecord record,
        DynamoDbStreamRecordContext context,
        CancellationToken cancellationToken)
    {
        var orderId = context.Keys["orderId"].S;
        var newStatus = context.NewImage["status"].S;

        return ValueTask.CompletedTask;
    }
}
```

`Keys`, `NewImage`, and `OldImage` deliberately expose the AWS DynamoDB `AttributeValue` model. The integration does not pretend DynamoDB images are ordinary JSON or impose a domain-object mapper; applications can map that model using whichever strategy fits their schema.

Record-processing exceptions are captured and translated into `StreamsEventResponse.BatchItemFailures` using each record's DynamoDB sequence number. Your Lambda event-source mapping must enable `ReportBatchItemFailures` for Lambda to honor partial batch responses. With stream sources, Lambda checkpoints at the lowest reported failed sequence number, so successfully processed records after that point may be delivered again and handlers should remain idempotent.

The context also exposes event id/name, source ARN, AWS region, sequence number, stream view type, approximate creation time, size, keys and images. `GetDynamoDbStreamRecord()` provides an explicit escape hatch back to the original AWS record.