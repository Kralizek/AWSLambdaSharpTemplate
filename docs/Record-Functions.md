# Record Functions

Record functions model Lambda invocations that contain multiple records. Source-specific packages use `RecordFunction<...>` as infrastructure and expose simpler public function/handler types for applications.

The runtime creates an invocation scope plus an independent dependency-injection scope per record. Each source package also defines its own result type derived from `LambdaRecordResult`, allowing AWS-specific success and failure semantics without forcing all integrations into one common result contract.

Applications should normally use the source-specific SQS, SNS, DynamoDB Streams, Kinesis Streams, or S3 function types rather than inheriting from `RecordFunction` directly.

See [Record Processing](Record-Processing.md) for scopes, parallelism, result handling, and retry semantics.
