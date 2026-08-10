# DynamoDB Streams sample

Use this sample when a Lambda is triggered by DynamoDB Streams and each stream record should be handled through the stream-specific programming model.

```text
DynamoDB table
  → DynamoDB Stream
  → Lambda event source mapping
  → DynamoDBEvent
  → DynamoDbStreamFunction<OrderChangeHandler>
  → OrderChangeHandler
```

## Minimal infrastructure

```hcl
resource "aws_dynamodb_table" "orders" {
  name         = "orders"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "id"

  attribute {
    name = "id"
    type = "S"
  }

  stream_enabled   = true
  stream_view_type = "NEW_AND_OLD_IMAGES"
}

resource "aws_lambda_event_source_mapping" "orders" {
  event_source_arn        = aws_dynamodb_table.orders.stream_arn
  function_name           = aws_lambda_function.sample.arn
  starting_position       = "LATEST"
  function_response_types = ["ReportBatchItemFailures"]

  # Increase this when more concurrency per shard is appropriate.
  parallelization_factor = 1
}
```

The Lambda definition, role, packaging, and IAM permissions are omitted. `stream_view_type` determines which images are available to the handler, while `ReportBatchItemFailures` enables sequence-number based partial failure reporting.

## Example Lambda input

```json
{
  "Records": [
    {
      "eventID": "1",
      "eventName": "MODIFY",
      "eventSource": "aws:dynamodb",
      "dynamodb": {
        "Keys": {
          "id": { "S": "A-123" }
        },
        "OldImage": {
          "id": { "S": "A-123" },
          "status": { "S": "Pending" }
        },
        "NewImage": {
          "id": { "S": "A-123" },
          "status": { "S": "Paid" }
        },
        "SequenceNumber": "111",
        "StreamViewType": "NEW_AND_OLD_IMAGES"
      }
    }
  ]
}
```

`Function` derives from `DynamoDbStreamFunction<OrderChangeHandler>`. The handler receives a `DynamoDbStreamItem` projection with keys, old/new images, sequence number, stream view type, and related metadata. The original AWS stream record remains available through `DynamoDbStreamRecordContext` when needed.

Failed `DynamoDbStreamRecordResult` values are translated into `StreamsEventResponse` entries using sequence numbers.

Records are deliberately processed sequentially inside one invocation. Increase throughput with the event source mapping's `ParallelizationFactor` rather than adding in-process parallelism, so shard ordering remains an infrastructure-level choice.

## Look at

- `Function` for the DynamoDB Streams specialization.
- `OrderChangeHandler` for projected stream data, context metadata, and record results.
- `aws-lambda-tools-defaults.json` for the function deployment defaults.

For another ordered stream whose record data is decoded from bytes, compare [KinesisStreamFunction](../KinesisStreamFunction/).
