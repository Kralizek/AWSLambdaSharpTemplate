# Raw SQS sample

Use this sample when the SQS record itself is part of your application contract and you do not want the framework to decode the message body first.

```text
SQS queue
  → Lambda event source mapping
  → SQSEvent
  → SqsFunction<RawSqsRecordHandler>
  → raw SQSMessage
```

## Minimal infrastructure

```hcl
resource "aws_sqs_queue" "orders" {
  name = "orders"
}

resource "aws_lambda_event_source_mapping" "orders" {
  event_source_arn        = aws_sqs_queue.orders.arn
  function_name           = aws_lambda_function.sample.arn
  function_response_types = ["ReportBatchItemFailures"]
}
```

The Lambda packaging, role, and permissions are omitted. The important part is that SQS is the Lambda event source and partial-batch reporting is enabled when individual failed messages should be retried independently.

## Example Lambda input

```json
{
  "Records": [
    {
      "messageId": "8e19c26b-1a46-4df3-bf33-08bc4377de2b",
      "body": "{\"orderId\":\"A-123\"}",
      "messageAttributes": {
        "tenant": {
          "stringValue": "north",
          "dataType": "String"
        }
      },
      "eventSource": "aws:sqs",
      "awsRegion": "eu-north-1"
    }
  ]
}
```

`Function` derives from `SqsFunction<RawSqsRecordHandler>`. The handler receives the original `SQSEvent.SQSMessage`, so the body, message attributes, message ID, and AWS delivery metadata remain available directly.

The record still participates in the normal SQS pipeline: one record scope per message, source-specific `SqsRecordResult` values, and partial-batch failure translation.

## Look at

- `Function` for the raw SQS specialization.
- `RawSqsRecordHandler` for direct access to the AWS SQS record.

For the application-oriented model where the message body is decoded into a POCO before handling, see [SqsFunction](../SqsFunction/).
