# Kralizek.Lambda.Template.DynamoDbStreams

Amazon DynamoDB Streams integration for `Kralizek.Lambda.Template`.

Use `DynamoDbStreamFunction<THandler>` to process stream records sequentially, with one dependency-injection scope per record.

```csharp
public sealed class Function
    : DynamoDbStreamFunction<OrderChangeHandler>;
```

Handlers receive a `DynamoDbStreamItem` representing the DynamoDB item change together with `DynamoDbStreamRecordContext` for delivery metadata:

```csharp
public sealed class OrderChangeHandler : IDynamoDbStreamRecordHandler
{
    public ValueTask HandleAsync(
        DynamoDbStreamItem item,
        DynamoDbStreamRecordContext context,
        CancellationToken cancellationToken)
    {
        var orderId = item.Keys["orderId"].S;
        var newStatus = item.NewImage["status"].S;

        return ValueTask.CompletedTask;
    }
}
```

`DynamoDbStreamItem` exposes the stream item's keys, new and old images, sequence number, stream view type, approximate creation time, and size. `Keys`, `NewImage`, and `OldImage` deliberately use the AWS DynamoDB `AttributeValue` model. The integration does not pretend DynamoDB images are ordinary JSON or impose a domain-object mapper; applications can map that model using whichever strategy fits their schema.

`DynamoDbStreamRecordContext` contains the outer record metadata such as event id/name, source ARN, and AWS region. The original AWS `DynamoDBEvent.DynamodbStreamRecord` is preserved in the context property bag and is available through `GetDynamoDbStreamRecord()` when AWS-specific details are needed.

Records within one Lambda invocation are processed sequentially by design. For additional throughput, configure `ParallelizationFactor` on the DynamoDB Streams event source mapping so Lambda can process multiple batches from a shard concurrently while preserving its stream-ordering guarantees.

Record-processing exceptions are captured and translated into `StreamsEventResponse.BatchItemFailures` using each record's DynamoDB sequence number. Your Lambda event-source mapping must enable `ReportBatchItemFailures` for Lambda to honor partial batch responses. With stream sources, Lambda checkpoints at the lowest reported failed sequence number, so successfully processed records after that point may be delivered again and handlers should remain idempotent.