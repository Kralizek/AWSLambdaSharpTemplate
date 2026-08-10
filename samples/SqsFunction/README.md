# SQS sample

Use this sample when SQS message bodies contain JSON that should be decoded into an application type before your handler runs.

```text
SQS queue
  → Lambda event source mapping
  → SQSEvent
  → SqsFunction<OrderCreated, OrderCreatedHandler>
  → OrderCreatedHandler
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

The Lambda function, IAM role, packaging, and queue permissions are omitted here so the example stays focused on the SQS-to-Lambda relationship. `ReportBatchItemFailures` is important when the function should report failed records without retrying successful records from the same batch.

## Example Lambda input

A trimmed SQS invocation looks like this:

```json
{
  "Records": [
    {
      "messageId": "8e19c26b-1a46-4df3-bf33-08bc4377de2b",
      "body": "{\"orderId\":\"A-123\"}",
      "eventSource": "aws:sqs",
      "awsRegion": "eu-north-1"
    }
  ]
}
```

The important boundary is the message body:

```text
SQSEvent
└── SQSMessage
    └── Body JSON
        └── OrderCreated
```

`Function` derives from `SqsFunction<OrderCreated, OrderCreatedHandler>`. The SQS integration decodes each `SQSMessage.Body` into `OrderCreated`, then the record pipeline delegates that record to `IRecordProcessor`, which creates an independent record scope, resolves and invokes the configured handler, and disposes the scope. The function translates `SqsRecordResult` values into the Lambda partial-batch response.

Normal SQS functions do not need to resolve `IRecordProcessor` directly. The public processor becomes useful when composing nested record envelopes while preserving the same per-record scope semantics, as shown by the SQS → SNS → S3 samples.

A failed record can therefore be reported without failing successful records from the same batch when the event source mapping enables partial batch responses.

## Look at

- `Function` for the typed SQS specialization.
- `OrderCreated` for the decoded application payload.
- `OrderCreatedHandler` for per-record processing and `SqsRecordResult`.

If your application needs the original `SQSEvent.SQSMessage` rather than a decoded body, see [RawSqsFunction](../RawSqsFunction/). For nested record composition, compare [SqsRawSnsS3Function](../SqsRawSnsS3Function/) and [SqsSnsS3Function](../SqsSnsS3Function/).
