# Record Functions

Record functions model Lambda invocations that contain multiple records. Source-specific packages use `RecordFunction<...>` as infrastructure and expose simpler public function/handler types for applications.

The runtime creates an invocation scope plus an independent dependency-injection scope per record. Each source package also defines its own result type derived from `LambdaRecordResult`, allowing AWS-specific success and failure semantics without forcing all integrations into one common result contract.

`RecordFunction<...>` owns the outer record pipeline: extracting records from the AWS envelope, selecting sequential or bounded-parallel scheduling, translating handler failures according to the event source, preserving each record/result association, and creating the final Lambda response.

Execution of one record is delegated to `IRecordProcessor<TRecord, TRecordResult, TContext>`. The processor owns only the record-local lifetime: create a scope, resolve the registered record handler, invoke it, and dispose the scope. `RecordFunction` itself uses this processor, so nested record composition and top-level record functions share the same handler activation and scoping behavior.

Applications should normally use the source-specific SQS, SNS, DynamoDB Streams, Kinesis Streams, or S3 function types rather than inheriting from `RecordFunction` directly. Direct use of `IRecordProcessor` is intended for advanced composition scenarios, such as an SQS message whose payload contains an S3 event with multiple inner records.

See [Record Processing](Record-Processing.md) for scopes, composition, parallelism, result handling, and retry semantics.
