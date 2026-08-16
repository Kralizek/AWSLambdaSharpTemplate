# SQS → SNS envelope → S3 sample

Use this sample when S3 notifications are published to SNS, delivered to SQS using the **standard SNS JSON envelope**, and finally consumed by Lambda. The SQS handler owns the topology-specific envelope decoding and delegates each inner S3 record to the framework's normal S3 record-processing pipeline.

```text
S3 bucket
  → SNS topic (standard delivery)
  → SQS queue
  → SQSEvent
  → SqsFunction<SqsSnsS3Handler>
  → SqsSnsS3Handler
      → decode SQSMessage.Body to SnsEnvelope
      → decode SnsEnvelope.Message to S3Event
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
  raw_message_delivery = false
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

The Lambda definition and packaging are omitted, as are the SNS topic policy that permits S3 to publish and the SQS queue policy that permits SNS to send messages. The important difference from the raw-delivery sample is `raw_message_delivery = false`, which preserves the SNS envelope in `SQSMessage.Body`.

## Example Lambda input

The SQS body contains an SNS notification whose `Message` property contains the serialized S3 event:

```json
{
  "Records": [
    {
      "messageId": "7f8c3ad7-3c2a-4dc6-95f4-bb6b0c6f0b52",
      "body": "{\"Type\":\"Notification\",\"MessageId\":\"2d402878-82d2-4c23-b5b9-6632f9f4fa71\",\"TopicArn\":\"arn:aws:sns:eu-north-1:123456789012:s3-events\",\"Message\":\"{\\\"Records\\\":[{\\\"eventSource\\\":\\\"aws:s3\\\",\\\"eventName\\\":\\\"ObjectCreated:Put\\\",\\\"s3\\\":{\\\"bucket\\\":{\\\"name\\\":\\\"example-uploads\\\"},\\\"object\\\":{\\\"key\\\":\\\"documents/report.pdf\\\",\\\"size\\\":12345}}}]}\"}",
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
    └── Body: SNS envelope
        └── Message: S3Event
            └── Records[]
                └── S3 record
```

## How the pieces fit

`Function` derives from the raw `SqsFunction<SqsSnsS3Handler>` specialization because the SQS body contains more than one nested application payload. The framework still owns SQS record iteration, per-message handling, and partial-batch failure translation.

`SqsSnsS3Handler` handles one outer SQS record. It decodes `SQSMessage.Body` to the sample's minimal `SnsEnvelope`, decodes `SnsEnvelope.Message` to `S3Event`, then iterates the inner S3 records. This keeps the complete SNS/S3 envelope chain in one topology-specific handler.

`SnsEnvelope` contains only the `Message` property because that is the only SNS metadata this sample needs. Applications can extend the model when they need more of the envelope.

`services.AddS3ObjectEventProcessing<S3ObjectEventHandler>()` registers the canonical S3 record processor. `SqsSnsS3Handler` calls that `IRecordProcessor` once for each inner S3 record, preserving the framework's S3 per-record DI scope, telemetry, adapter behavior, and `S3RecordContext` creation.

`S3ObjectEventHandler` remains an ordinary `IS3ObjectEventHandler`. Its `S3RecordContext` carries forward the outer context properties, so `context.GetSqsMessage()` can still recover the containing SQS message without sharing scoped services between the outer and inner handlers.

Inner S3 records are processed sequentially and fail fast. The AWS retry/acknowledgement boundary remains the outer SQS message: if one inner S3 record throws, the containing SQS message is reported as failed; unrelated SQS messages can still succeed.

## Look at

- `Function` for raw SQS hosting plus SNS/S3 decoder and S3 processor registration.
- `SqsSnsS3Handler` for the complete nested-envelope decoding and inner-record orchestration.
- `SnsEnvelope` for the minimal preserved SNS envelope.
- `S3ObjectEventHandler` for the normal S3 application-handler contract and propagated SQS context.

For the same topology with SNS Raw Message Delivery enabled, compare [SqsRawSnsS3Function](../SqsRawSnsS3Function/). In that shape the SQS body is already an `S3Event`, so the typed `SqsFunction<S3Event, ...>` programming model remains the natural fit.
