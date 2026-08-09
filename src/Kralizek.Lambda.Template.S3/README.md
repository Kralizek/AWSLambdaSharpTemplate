# Kralizek.Lambda.Template.S3

Support for AWS Lambda functions processing native Amazon S3 event notifications and S3 Batch Operations.

## Event notifications

Derive from `S3Function<THandler>` and implement `IS3ObjectEventHandler`. The handler receives a synthetic `S3ObjectEvent` containing a decoded `S3ObjectReference`, the event name, event time, and sequencer. The original AWS notification record remains available through `S3RecordContext.GetS3EventRecord()`.

## S3 Batch Operations

Derive from `S3BatchFunction<THandler>` and implement `IS3BatchItemHandler`. Batch invocation schema 2.0 is required.

The Batch handler receives an `S3BatchItem`. The initial task-key implementation is `S3BatchObjectKey`, which composes the shared `S3ObjectReference` value object. The raw Batch request and task remain available through `S3BatchContext`.

Return `S3BatchResult.Succeeded()`, `S3BatchResult.TemporaryFailure()`, or `S3BatchResult.PermanentFailure()` for each task. Unexpected exceptions fail the Lambda invocation rather than having retry semantics inferred by the framework.
