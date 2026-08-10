# Kinesis Streams sample

Use this sample when Kinesis records contain binary payloads that should be decoded into an application type before the handler runs.

`Function` derives from `KinesisStreamFunction<OrderCreated, OrderCreatedHandler>`. The framework decodes each record payload through `IBinaryPayloadDecoder<OrderCreated>`, invokes the record handler, and translates failed `KinesisStreamRecordResult` values into `StreamsEventResponse` batch item failures using sequence numbers.

Records are processed sequentially inside one Lambda invocation. Increase stream concurrency through the Lambda event source mapping rather than adding in-process parallelism, so ordering semantics stay under the event source's control.

## Look at

- `Function` for the typed Kinesis specialization.
- `OrderCreated` for the decoded record payload.
- `OrderCreatedHandler` for record metadata, handling, and source-specific results.

For a stream where the framework projects DynamoDB keys and images instead of decoding a binary payload, compare [DynamoDbStreamFunction](../DynamoDbStreamFunction/).
