# SNS sample

Use this sample when SNS notification messages contain JSON that should be decoded into an application type before your handler runs.

`Function` derives from `SnsFunction<OrderCreated, OrderCreatedHandler>`. The framework processes each SNS record independently and decodes the notification message into `OrderCreated` before invoking the handler.

Unlike SQS and stream integrations, SNS does not have a partial batch response protocol. An unhandled record failure therefore fails the Lambda invocation rather than returning a list of failed item identifiers.

## Look at

- `Function` for the typed SNS specialization.
- `OrderCreated` for the decoded notification payload.
- `OrderCreatedHandler` for per-record handling and `SnsRecordResult`.

If you need the original SNS record and message attributes instead of a decoded payload, see [RawSnsFunction](../RawSnsFunction/).
