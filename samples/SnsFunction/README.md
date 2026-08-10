# SNS sample

Use this sample when SNS notification messages contain JSON that should be decoded into an application type before your handler runs.

```text
SNS topic
  → Lambda subscription
  → SNSEvent
  → SnsFunction<OrderCreated, OrderCreatedHandler>
  → OrderCreatedHandler
```

## Minimal infrastructure

```hcl
resource "aws_sns_topic" "orders" {
  name = "orders"
}

resource "aws_sns_topic_subscription" "lambda" {
  topic_arn = aws_sns_topic.orders.arn
  protocol  = "lambda"
  endpoint  = aws_lambda_function.sample.arn
}

resource "aws_lambda_permission" "sns" {
  statement_id  = "AllowExecutionFromSns"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.sample.function_name
  principal     = "sns.amazonaws.com"
  source_arn    = aws_sns_topic.orders.arn
}
```

The Lambda function definition, role, and packaging are omitted.

## Example Lambda input

```json
{
  "Records": [
    {
      "EventSource": "aws:sns",
      "Sns": {
        "MessageId": "2a6d0ec1-7c87-4df2-a92c-8c6a4f880e21",
        "Subject": "order-created",
        "Message": "{\"orderId\":\"A-123\"}",
        "MessageAttributes": {}
      }
    }
  ]
}
```

The application payload lives inside `Sns.Message`:

```text
SNSEvent
└── SNSRecord
    └── Sns.Message JSON
        └── OrderCreated
```

`Function` derives from `SnsFunction<OrderCreated, OrderCreatedHandler>`. The framework decodes the notification message into `OrderCreated`, then delegates the record to `IRecordProcessor`, which creates the independent record scope and resolves/invokes the configured handler. Applications normally do not call the processor directly; it is the shared one-record execution primitive used by `RecordFunction` and advanced nested-record composition.

Unlike SQS and stream integrations, SNS has no partial-batch response protocol. An unhandled failure therefore fails the Lambda invocation rather than returning failed item identifiers.

## Look at

- `Function` for the typed SNS specialization.
- `OrderCreated` for the decoded notification payload.
- `OrderCreatedHandler` for per-record handling and `SnsRecordResult`.

If you need the original SNS record and message attributes instead of a decoded payload, see [RawSnsFunction](../RawSnsFunction/).
