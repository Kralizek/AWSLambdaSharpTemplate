# SQS → SNS envelope → S3 sample

Use this sample when S3 notifications are published to SNS, delivered to SQS using the **standard SNS JSON envelope**, and finally consumed by Lambda. It demonstrates the extra decoding layer plus nested S3 record composition.

```text
S3 bucket
  → SNS topic (standard delivery)
  → SQS queue
  → SQSEvent
  → SqsFunction<SnsEnvelope, SnsEnvelopedS3DeliveryHandler>
  → decode SnsEnvelope.Message to S3Event
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

`Function` derives from `SqsFunction<SnsEnvelope, SnsEnvelopedS3DeliveryHandler>`, so the outer SQS integration first decodes the body into the sample's minimal `SnsEnvelope` model.

## How the pieces fit

`SnsEnvelope` contains only the `Message` property because that is the only SNS metadata this sample needs. Applications can extend the model when they need more of the envelope.

`SnsEnvelopedS3DeliveryHandler` handles one **outer SQS record**. It uses `IStringPayloadDecoder<S3Event>` to decode `SnsEnvelope.Message`, then passes the resulting S3 event to `S3EventDispatcher`. This second decoding step is the main application difference from raw SNS delivery.

`S3EventDispatcher` expands `S3Event.Records` and calls `IRecordProcessor` once for each inner S3 record. The processor gives every inner record its own DI scope and runs the same canonical S3 adapter/application-handler path used by direct S3 functions.

`services.AddS3ObjectEventProcessing<S3ObjectEventHandler>()` registers that S3 record-processing path, while the explicit `IStringPayloadDecoder<S3Event>` registration handles the SNS `Message` string.

`S3ObjectEventHandler` remains an ordinary `IS3ObjectEventHandler`. Its `S3RecordContext` carries forward the outer context properties, so `context.GetSqsMessage()` can still recover the containing SQS message without sharing scoped services between the outer and inner handlers.

Inner S3 records are processed sequentially and fail fast. The AWS retry/acknowledgement boundary remains the outer SQS message: if one inner S3 record throws, the containing SQS message is reported as failed; unrelated SQS messages can still succeed.

## Look at

- `Function` for the outer SQS specialization, S3 processor registration, and S3 decoder registration.
- `SnsEnvelope` for the minimal preserved SNS envelope.
- `SnsEnvelopedS3DeliveryHandler` for the second decoding layer.
- `S3EventDispatcher` for nested iteration through `IRecordProcessor`.
- `S3ObjectEventHandler` for the normal S3 application-handler contract and propagated SQS context.

For the same topology with SNS Raw Message Delivery enabled, compare [SqsRawSnsS3Function](../SqsRawSnsS3Function/). The infrastructure difference is essentially one subscription setting; the payload and decoding path are what change.
