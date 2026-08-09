# Kinesis Streams

Use `Kralizek.Lambda.Template.KinesisStreams` for Lambda functions consuming Amazon Kinesis Data Streams.

## Typed payloads

```csharp
public sealed class Function
    : KinesisStreamFunction<OrderCreated, OrderCreatedHandler>;
```

Typed functions decode `KinesisEventRecord.Kinesis.Data` through `IBinaryPayloadDecoder<TPayload>`. `JsonBinaryPayloadDecoder<TPayload>` is registered by default; replace it in `ConfigureServices` when the stream carries another binary format.

Use `KinesisStreamFunction<THandler>` when the handler needs the raw AWS `KinesisEventRecord`. The original record also remains available from the Kinesis record context for typed handlers.

## Results and retry behavior

Handlers return `KinesisStreamRecordResult.Success` or `KinesisStreamRecordResult.Failed(reason)`. Failed records are translated into `StreamsEventResponse` entries using Kinesis sequence numbers. Enable `ReportBatchItemFailures` on the Lambda event source mapping for partial-batch responses to take effect.

Lambda checkpoints at the lowest failed sequence number, so records after that point can be delivered again even if they were successfully processed. Handlers should therefore be idempotent.

Records are processed sequentially inside one invocation. Configure concurrency, including `ParallelizationFactor`, on the Lambda event source mapping so partition ordering remains an infrastructure-level concern.
