# SQS sample

Use this sample when SQS message bodies contain JSON that should be decoded into an application type before your handler runs.

`Function` derives from `SqsFunction<OrderCreated, OrderCreatedHandler>`. The framework processes the SQS batch record by record, decodes each message body into `OrderCreated`, creates the record handler scope, and translates `SqsRecordResult` values into the Lambda partial batch response.

A failed record can therefore be reported without failing successful records from the same batch when the event source mapping is configured for partial batch responses.

## Look at

- `Function` for the typed SQS specialization.
- `OrderCreated` for the decoded application payload.
- `OrderCreatedHandler` for per-record processing and `SqsRecordResult`.

If your application needs the original `SQSEvent.SQSMessage` rather than a decoded body, see [RawSqsFunction](../RawSqsFunction/).
