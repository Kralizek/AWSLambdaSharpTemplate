# DynamoDB Streams

Use `Kralizek.Lambda.Template.DynamoDbStreams` for functions consuming DynamoDB Streams.

```csharp
public sealed class Function
    : DynamoDbStreamFunction<OrderChangeHandler>;
```

Handlers implement `IDynamoDbStreamRecordHandler` and receive a `DynamoDbStreamItem` plus `DynamoDbStreamRecordContext`.

`DynamoDbStreamItem` exposes keys, old/new images, sequence number, stream view type, approximate creation time, and size. DynamoDB values deliberately remain in AWS's `AttributeValue` representation rather than being treated as generic JSON. The original AWS stream record remains available through `GetDynamoDbStreamRecord()`.

## Ordering and failures

Handlers return `DynamoDbStreamRecordResult.Success` or `DynamoDbStreamRecordResult.Failed(reason)`. Failed records become `StreamsEventResponse.BatchItemFailures` using DynamoDB sequence numbers. Enable `ReportBatchItemFailures` on the event source mapping.

Records are processed sequentially by design. Use the event source mapping's `ParallelizationFactor` when more throughput is needed while preserving stream ordering semantics. Because stream checkpoints use the lowest failed sequence number, successfully processed records after that point may be delivered again; handlers should be idempotent.
