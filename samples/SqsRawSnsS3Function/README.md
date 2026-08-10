# SQS → SNS raw delivery → S3 sample

Use this sample when S3 notifications are published to SNS, delivered to SQS with **Raw Message Delivery enabled**, and finally consumed by Lambda. It demonstrates nested record composition while keeping the normal S3 handler and per-record DI scope semantics.

```text
S3 bucket
  → SNS topic (raw message delivery)
  → SQS queue
  → SQSEvent
  → SqsFunction<S3Event, S3DeliveryHandler>
  → S3EventDispatcher
  → IRecordProcessor<S3 record, S3RecordResult, RecordContext>
  → S3ObjectEventHandler
```

## Minimal infrastructure

```hcl
resource "aws_s3_bucket" "uploads" {
  bucket = "example-uploads"
}

resource "aws_sns_topic" "s3_events" {
  name = "s3-events"
}

resource "aws_sqs_queue" "events" {
  name = "s3-events"
}

resource "aws_sns_topic_subscription" "sqs" {
  topic_arn            = aws_sns_topic.s3_events.arn
  protocol             = "sqs"
  endpoint             = aws_sqs_queue.events.arn
  raw_message_delivery = true
}

resource "aws_s3_bucket_notification" "uploads" {
  bucket = aws_s3_bucket.uploads.id

  topic {
    topic_arn = aws_sns_topic.s3_events.arn
    events    = ["s3:ObjectCreated:*"]
  }
}

resource "aws_lambda_event_source_mapping" "events" {
  event_source_arn        = aws_sqs_queue.events.arn
  function_name           = aws_lambda_function.sample.arn
  function_response_types = ["ReportBatchItemFailures"]
}
```

The Lambda definition and packaging are omitted, as are the SNS topic policy that permits S3 to publish and the SQS queue policy that permits the SNS topic to send messages. The important switch is `raw_message_delivery = true`: SNS removes its envelope before placing the message on SQS.

## Example Lambda input

The SQS body contains the serialized S3 event directly:

```json
{
  "Records": [
    {
      "messageId": "c5a3f4d9-9c33-4f9d-bd8e-4e3ab03b2c3f",
      "body": "{\"Records\":[{\"eventSource\":\"aws:s3\",\"eventName\":\"ObjectCreated:Put\",\"s3\":{\"bucket\":{\"name\":\"example-uploads\"},\"object\":{\"key\":\"documents/report.pdf\",\"size\":12345}}}]}",
      "eventSource": "aws:sqs",
      "eventSourceARN": "arn:aws:sqs:eu-north-1:123456789012:s3-events",
      "awsRegion": "eu-north-1"
    }
  ]
}
```

Conceptually:

```text
SQSEvent
└── SQSMessage
    └── Body: S3Event
        └── Records[]
            └── S3 record
```

`Function` derives from `SqsFunction<S3Event, S3DeliveryHandler>`, so the outer SQS integration decodes `SQSMessage.Body` directly into `S3Event`.

## How the pieces fit

`S3DeliveryHandler` handles one **outer SQS record**. It does not process S3 records itself; it passes the decoded `S3Event` to `S3EventDispatcher` and returns `SqsRecordResult.Success` only after all inner records succeed.

`S3EventDispatcher` expands `S3Event.Records` and calls `IRecordProcessor` once for each inner S3 record. Calling the S3 handler directly would make all inner records share the outer SQS handler scope; the processor instead creates an independent scope per S3 record, resolves the canonical S3 adapter and application handler inside it, invokes them, and disposes the scope.

`services.AddS3ObjectEventProcessing<S3ObjectEventHandler>()` registers that canonical S3 processing path. The application therefore continues to implement `IS3ObjectEventHandler`, just as it would for a Lambda triggered directly by S3.

`S3ObjectEventHandler` receives `S3ObjectEvent` and `S3RecordContext`. The inner context copies the outer `RecordContext` properties, so `context.GetSqsMessage()` can recover the containing SQS message even though the inner S3 handler runs in a separate DI scope.

Inner records are deliberately processed sequentially and fail fast. AWS acknowledges and retries the **outer SQS message**, not an individual S3 record hidden inside its body. An inner exception therefore marks the containing SQS message as failed while unrelated SQS messages in the same invocation can still succeed.

## Look at

- `Function` for S3 processor registration and the outer typed SQS specialization.
- `S3DeliveryHandler` for the outer SQS acknowledgement boundary.
- `S3EventDispatcher` for nested iteration through `IRecordProcessor`.
- `S3ObjectEventHandler` for the normal S3 application-handler contract and propagated SQS context.

For the same topology with the normal SNS JSON envelope preserved in SQS, compare [SqsSnsS3Function](../SqsSnsS3Function/).
