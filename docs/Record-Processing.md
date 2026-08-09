# Record Processing

Record-oriented integrations share a common dispatch model while preserving source-specific AWS semantics.

## Scopes

A record invocation has one invocation scope plus an independent nested dependency-injection scope per record. Scoped handler dependencies therefore remain isolated between records.

## Sequential and parallel processing

SQS and SNS expose sequential functions by default and bounded-parallel variants when concurrency inside an invocation is appropriate.

DynamoDB Streams and Kinesis Streams deliberately process records sequentially. Increase throughput with the event source mapping's `ParallelizationFactor` rather than reordering work inside one batch. S3 notifications and S3 Batch Operations are also sequential in the current implementation.

## Results and failures

Record handlers return source-specific result types derived from `LambdaRecordResult`. This keeps success/failure semantics explicit without forcing unrelated AWS sources into one result contract.

Failure behavior depends on the event source:

- SQS uses `SqsRecordResult.Success` / `Failed(reason)` and translates failed records into `SQSBatchResponse.batchItemFailures`.
- DynamoDB Streams uses `DynamoDbStreamRecordResult` and reports failed sequence numbers through `StreamsEventResponse`.
- Kinesis Streams uses `KinesisStreamRecordResult` and reports failed sequence numbers through `StreamsEventResponse`.
- SNS returns `SnsRecordResult.Completed`; exceptions fail the whole invocation because SNS has no partial-batch response.
- S3 Batch Operations uses explicit succeeded, temporary-failure, and permanent-failure results defined by the S3 Batch protocol.

Invocation cancellation is not treated as an ordinary record failure. It aborts the invocation so AWS can apply its retry behavior.
