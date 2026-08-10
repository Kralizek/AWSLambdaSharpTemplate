# DynamoDB Streams sample

Use this sample when a Lambda is triggered by DynamoDB Streams and each record should be handled through the stream-specific programming model.

`Function` derives from `DynamoDbStreamFunction<OrderChangeHandler>`. The handler receives a `DynamoDbStreamItem` projection together with `DynamoDbStreamRecordContext`, while the raw AWS stream record remains available through the context when needed.

The specialization returns source-specific `DynamoDbStreamRecordResult` values and translates failed records into `StreamsEventResponse` batch item failures using sequence numbers.

Records are processed sequentially inside one Lambda invocation. If you need more concurrency, configure the Lambda event source mapping's `ParallelizationFactor` rather than adding in-process parallelism, so stream ordering semantics remain explicit.

## Look at

- `Function` for the DynamoDB Streams specialization.
- `OrderChangeHandler` for keys, old/new images, stream metadata, and record results.
- `aws-lambda-tools-defaults.json` for example deployment settings; event source mapping options such as `ReportBatchItemFailures` are infrastructure concerns.

For another ordered stream with decoded record payloads, compare [KinesisStreamFunction](../KinesisStreamFunction/).
