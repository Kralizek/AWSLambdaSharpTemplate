# Record Processing

Record-oriented integrations share a common dispatch model while preserving source-specific AWS semantics.

## Scopes

A record invocation has one invocation scope plus an independent dependency-injection scope per record. Scoped handler dependencies therefore remain isolated between records.

The per-record lifetime is implemented by `IRecordProcessor<TRecord, TRecordResult, TContext>`. For each call to `ProcessAsync`, the processor creates a fresh scope, resolves the configured `IRecordHandler<TRecord, TRecordResult, TContext>` from that scope, invokes it, and disposes the scope.

Microsoft dependency-injection scopes are not hierarchical. A nested record scope does not inherit scoped instances from an outer record or invocation scope. State that must cross a composition boundary should therefore travel explicitly through the framework context rather than through scoped services.

## Nested record composition

A record processor can be injected when a record payload contains another record-oriented envelope. For example, an SQS message may contain an S3 event with several S3 records. Dispatching each inner S3 record through the registered S3 record processor gives every inner record the same isolated handler scope it would receive in a direct S3 function.

The processor intentionally handles only one record. The caller owns iteration of the nested envelope, while the outer `RecordFunction` continues to own scheduling, exception translation, and the AWS acknowledgement/checkpoint response.

This means the AWS retry boundary does not move inward merely because an inner envelope is processed. If an inner S3 record throws while processing an SQS message, the exception propagates to the SQS record pipeline and the containing SQS message is the failed item. For fail-fast inner iteration, the remaining inner records in that message are skipped and will be retried with the containing message.

Source packages may expose registration helpers that hide their internal adapters. For example, S3 exposes `AddS3ObjectEventProcessing<THandler>()` so composed pipelines can obtain the canonical S3 record processor while application code continues to implement `IS3ObjectEventHandler`.

## Sequential and parallel processing

SQS and SNS expose sequential functions by default and bounded-parallel variants when concurrency inside an invocation is appropriate.

DynamoDB Streams and Kinesis Streams deliberately process records sequentially. Increase throughput with the event source mapping's `ParallelizationFactor` rather than reordering work inside one batch. S3 notifications and S3 Batch Operations are also sequential in the current implementation.

`IRecordProcessor` does not select a scheduling policy and does not provide a `ProcessManyAsync` API. Scheduling remains an envelope-level concern so source-specific ordering and retry semantics stay explicit.

## Results and failures

Record handlers return source-specific result types derived from `LambdaRecordResult`. This keeps success/failure semantics explicit without forcing unrelated AWS sources into one result contract.

Failure behavior depends on the event source:

- SQS uses `SqsRecordResult.Success` / `Failed(reason)` and translates failed records into `SQSBatchResponse.batchItemFailures`.
- DynamoDB Streams uses `DynamoDbStreamRecordResult` and reports failed sequence numbers through `StreamsEventResponse`.
- Kinesis Streams uses `KinesisStreamRecordResult` and reports failed sequence numbers through `StreamsEventResponse`.
- SNS returns `SnsRecordResult.Completed`; exceptions fail the whole invocation because SNS has no partial-batch response.
- S3 Batch Operations uses explicit succeeded, temporary-failure, and permanent-failure results defined by the S3 Batch protocol.

Invocation cancellation is not treated as an ordinary record failure. It aborts the invocation so AWS can apply its retry behavior.
