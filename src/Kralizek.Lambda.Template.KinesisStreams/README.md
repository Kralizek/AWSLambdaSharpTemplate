# Kralizek.Lambda.Template.KinesisStreams

Support for AWS Lambda functions triggered by Amazon Kinesis Data Streams.

Use `KinesisStreamFunction<THandler>` when the handler needs the raw AWS record, or `KinesisStreamFunction<TPayload, THandler>` when the record data should be decoded into an application contract.

Typed payloads use `IBinaryPayloadDecoder<TPayload>` and default to `JsonBinaryPayloadDecoder<TPayload>`. Register another decoder in `ConfigureServices` to handle protobuf, MessagePack or another binary format.

Handlers return `KinesisStreamRecordResult.Success` or `KinesisStreamRecordResult.Failed(reason)`. Failed results are translated into `StreamsEventResponse` entries using the record sequence number. Configure the Lambda event source mapping with `ReportBatchItemFailures` for partial batch responses to take effect.

When multiple records fail, the response includes every failed sequence number. Lambda uses the lowest failed sequence number as the checkpoint and retries from that point, so later records in the same batch can be delivered again. Handlers should therefore be safe to retry.

Records are processed sequentially inside an invocation. Configure Kinesis/Lambda event source mapping concurrency, including `ParallelizationFactor`, at the infrastructure layer rather than through the function implementation.