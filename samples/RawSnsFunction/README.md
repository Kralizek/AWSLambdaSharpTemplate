# Raw SNS sample

Use this sample when the SNS record itself is the useful input and you do not want the framework to decode the notification message into an application type.

`Function` derives from `SnsFunction<RawSnsRecordHandler>`. The handler receives the raw SNS record, which keeps the notification message, subject, message attributes, and other AWS metadata available without an intermediate payload model.

SNS processing still uses one handler invocation per record, but SNS has no partial batch response protocol: an unhandled failure fails the Lambda invocation.

## Look at

- `Function` for the raw SNS specialization.
- `RawSnsRecordHandler` for direct access to the AWS SNS record.

For JSON notification payloads decoded into a POCO before handling, see [SnsFunction](../SnsFunction/).
