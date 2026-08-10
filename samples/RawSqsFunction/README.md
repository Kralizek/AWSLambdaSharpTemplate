# Raw SQS sample

Use this sample when the SQS record itself is part of your application contract and you do not want the framework to decode the message body first.

`Function` derives from `SqsFunction<RawSqsRecordHandler>`. The handler receives the raw `SQSEvent.SQSMessage`, so it can work directly with the body, message attributes, IDs, and other AWS-provided metadata.

You still keep the record-oriented SQS behavior, including source-specific results and partial batch failure handling.

## Look at

- `Function` for the raw SQS specialization.
- `RawSqsRecordHandler` for direct access to the AWS SQS record.

For the more application-oriented model where a JSON body is decoded into a POCO, see [SqsFunction](../SqsFunction/).
