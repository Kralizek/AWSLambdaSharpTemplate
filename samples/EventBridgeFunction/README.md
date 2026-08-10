# EventBridge sample

Use this sample when an EventBridge event has a strongly typed `detail` payload but the handler still needs the EventBridge envelope.

```text
EventBridge event
  → rule
  → Lambda target
  → CloudWatchEvent<OrderCreated>
  → EventBridgeFunction<OrderCreated, OrderCreatedHandler>
```

## Minimal infrastructure

```hcl
resource "aws_cloudwatch_event_rule" "orders" {
  name = "order-created"

  event_pattern = jsonencode({
    source      = ["com.example.orders"]
    detail-type = ["Order Created"]
  })
}

resource "aws_cloudwatch_event_target" "lambda" {
  rule = aws_cloudwatch_event_rule.orders.name
  arn  = aws_lambda_function.sample.arn
}

resource "aws_lambda_permission" "eventbridge" {
  statement_id  = "AllowExecutionFromEventBridge"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.sample.function_name
  principal     = "events.amazonaws.com"
  source_arn    = aws_cloudwatch_event_rule.orders.arn
}
```

The Lambda function definition, role, and packaging are omitted.

## Example Lambda input

```json
{
  "version": "0",
  "id": "7bf73129-1428-4cd3-a780-95db273d1602",
  "detail-type": "Order Created",
  "source": "com.example.orders",
  "account": "123456789012",
  "time": "2026-08-10T05:00:00Z",
  "region": "eu-north-1",
  "resources": [],
  "detail": {
    "orderId": "A-123"
  }
}
```

The distinction from a generic event is important: `detail` is the application payload, while `source`, `detail-type`, event ID, time, and other fields remain part of the envelope.

`Function` derives from `EventBridgeFunction<OrderCreated, OrderCreatedHandler>`. The Lambda serializer materializes `CloudWatchEvent<OrderCreated>`, so there is no separate payload-decoder step. The handler can use both `input.Detail` and the EventBridge metadata.

## Look at

- `Function` for the EventBridge specialization.
- `OrderCreated` for the typed `detail` contract.
- `OrderCreatedHandler` for using the application payload together with envelope metadata.

For a source-neutral event with no EventBridge envelope, see [EventFunction](../EventFunction/).
