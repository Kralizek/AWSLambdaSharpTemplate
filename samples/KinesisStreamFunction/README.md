# Kinesis Streams sample

Use this sample when Kinesis records contain binary payloads that should be decoded into an application type before the handler runs.

```text
Kinesis stream
  → Lambda event source mapping
  → KinesisEvent
  → KinesisStreamFunction<OrderCreated, OrderCreatedHandler>
  → OrderCreatedHandler
```

## Minimal infrastructure

```hcl
resource "aws_kinesis_stream" "orders" {
  name        = "orders"
  shard_count = 1
}

resource "aws_lambda_event_source_mapping" "orders" {
  event_source_arn        = aws_kinesis_stream.orders.arn
  function_name           = aws_lambda_function.sample.arn
  starting_position       = "LATEST"
  function_response_types = ["ReportBatchItemFailures"]

  # Increase this when more concurrency per shard is appropriate.
  parallelization_factor = 1
}
```

The Lambda function, role, packaging, and IAM permissions are omitted. Partial-batch reporting uses Kinesis sequence numbers; concurrency remains an event-source-mapping concern.

## Example Lambda input

Kinesis places the application payload in `kinesis.data` as Base64-encoded bytes. If the logical payload is:

```json
{
  "orderId": "A-123"
}
```

Lambda receives an envelope shaped like:

```json
{
  "Records": [
    {
      "eventSource": "aws:kinesis",
      "eventID": "shardId-000000000000:111",
      "kinesis": {
        "partitionKey": "orders",
        "sequenceNumber": "111",
        "data": "eyJvcmRlcklkIjoiQS0xMjMifQ=="
      }
    }
  ]
}
```

Conceptually:

```text
KinesisEvent
└── KinesisEventRecord
    └── Data bytes
        └── OrderCreated
```

`Function` derives from `KinesisStreamFunction<OrderCreated, OrderCreatedHandler>`. The framework passes the record bytes through `IBinaryPayloadDecoder<OrderCreated>` before invoking the handler. The default decoder handles JSON, and applications can replace it through dependency injection.

Failed `KinesisStreamRecordResult` values become `StreamsEventResponse` batch item failures using sequence numbers.

Records are deliberately processed sequentially inside one invocation. Increase concurrency through `ParallelizationFactor` on the event source mapping rather than reordering records inside the function.

## Look at

- `Function` for the typed Kinesis specialization.
- `OrderCreated` for the decoded payload.
- `OrderCreatedHandler` for Kinesis metadata, handling, and source-specific results.

For a stream where the framework projects DynamoDB keys and images instead of decoding binary data, compare [DynamoDbStreamFunction](../DynamoDbStreamFunction/).
