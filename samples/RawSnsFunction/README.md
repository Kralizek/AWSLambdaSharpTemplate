# Raw SNS sample

Use this sample when the SNS record itself is the useful input and you do not want the framework to decode the notification message into an application type.

```text
SNS topic
  → Lambda subscription
  → SNSEvent
  → SnsFunction<RawSnsRecordHandler>
  → raw SNSRecord
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
        "MessageAttributes": {
          "tenant": {
            "Type": "String",
            "Value": "north"
          }
        }
      }
    }
  ]
}
```

`Function` derives from `SnsFunction<RawSnsRecordHandler>`. The handler receives the original SNS record, keeping the message, subject, message attributes, identifiers, and AWS metadata available directly.

SNS still uses one handler invocation per record. Under the hood, `IRecordProcessor` creates and disposes the independent scope for that record and resolves `RawSnsRecordHandler` inside it. The processor does not own SNS scheduling or failure semantics; those remain with the outer function pipeline. Because SNS has no partial-batch response protocol, an unhandled failure fails the Lambda invocation.

## Look at

- `Function` for the raw SNS specialization.
- `RawSnsRecordHandler` for direct access to the AWS SNS record.

For JSON notification payloads decoded into a POCO before handling, see [SnsFunction](../SnsFunction/).
