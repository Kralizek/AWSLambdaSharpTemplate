# SQS → SNS raw delivery → S3

This sample demonstrates processing S3 object notifications that reach Lambda through an SNS subscription to SQS with **Raw Message Delivery enabled**.

The Lambda event source is SQS. SNS is part of the delivery topology, but because raw delivery is enabled the SNS JSON envelope is removed before the message is written to the queue. The SQS message body therefore contains the S3 event directly.

```text
S3 notification
    ↓
SNS topic
    ↓ Raw Message Delivery
SQS message body = S3Event JSON
    ↓
Lambda
    ↓
SqsFunction<S3Event, S3DeliveryHandler>
    ↓
S3EventDispatcher
    ↓
IRecordProcessor<S3EventNotificationRecord, S3RecordResult, RecordContext>
    ↓
IS3ObjectEventHandler
```

## Example infrastructure topology

The sample assumes this AWS flow:

```text
S3 bucket
  → SNS topic
  → SQS queue
  → Lambda
```

The important detail is that the SNS subscription to SQS uses **Raw Message Delivery**. That causes the SQS message body to contain the serialized `S3Event` directly.

A minimal Terraform sketch looks like this:

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
  topic_arn = aws_sns_topic.s3_events.arn
  protocol  = "sqs"
  endpoint  = aws_sqs_queue.events.arn

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

A complete deployment also needs an SNS topic policy that allows S3 to publish, an SQS queue policy that allows the SNS topic to call `sqs:SendMessage`, and the usual Lambda packaging and permission resources. Those details are omitted so the sample stays focused on the event-processing topology.

## Example Lambda input

Because `raw_message_delivery = true`, the function receives an `SQSEvent` whose `Records[*].body` is already an `S3Event` JSON payload.

A trimmed example looks like this:

```json
{
  "Records": [
    {
      "messageId": "c5a3f4d9-9c33-4f9d-bd8e-4e3ab03b2c3f",
      "receiptHandle": "...",
      "body": "{\"Records\":[{\"eventSource\":\"aws:s3\",\"eventName\":\"ObjectCreated:Put\",\"s3\":{\"bucket\":{\"name\":\"example-uploads\"},\"object\":{\"key\":\"documents/report.pdf\",\"size\":12345}}}]}",
      "attributes": {
        "ApproximateReceiveCount": "1"
      },
      "messageAttributes": {},
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

That shape maps directly to:

```csharp
SqsFunction<S3Event, S3DeliveryHandler>
```

The SQS integration decodes the body directly into `S3Event`; no SNS-envelope decoding step is needed inside the function.

## Function

`Function` derives from:

```csharp
SqsFunction<S3Event, S3DeliveryHandler>
```

The SQS integration therefore decodes each `SQSMessage.Body` directly into an AWS `S3Event`. There is no SNS-envelope decoder in this sample because Raw Message Delivery has already removed that envelope.

`ConfigureServices` adds two pieces needed for the nested S3 pipeline:

```csharp
services.AddS3ObjectEventProcessing<S3ObjectEventHandler>();
services.TryAddScoped<S3EventDispatcher>();
```

`AddS3ObjectEventProcessing<THandler>()` registers the same S3 record-processing adapter used by a direct `S3Function<THandler>`. This is what lets the nested pipeline keep the normal `IS3ObjectEventHandler` programming model instead of reimplementing S3-specific context and object mapping inside the SQS handler.

## S3DeliveryHandler

`S3DeliveryHandler` is the handler for one **outer SQS record**:

```csharp
ISqsMessageHandler<S3Event>
```

At this point the SQS body has already been decoded into an `S3Event`. The handler does not process individual S3 records itself; it delegates the event to `S3EventDispatcher`.

If every inner S3 record completes successfully, the handler returns `SqsRecordResult.Success` and the containing SQS message can be acknowledged normally.

## S3EventDispatcher

An `S3Event` can contain multiple S3 notification records. `S3EventDispatcher` expands that inner collection and sends each record through:

```csharp
IRecordProcessor<
    S3Event.S3EventNotificationRecord,
    S3RecordResult,
    RecordContext>
```

The record processor is important here because simply resolving `S3ObjectEventHandler` from the SQS handler scope and calling it repeatedly would make every inner S3 record share the outer SQS record's scoped dependencies.

Instead, every call to `ProcessAsync` creates an independent record scope, resolves the canonical S3 adapter and application handler inside that scope, invokes them, and disposes the scope. Inner S3 records therefore have the same per-record lifetime semantics they would have if Lambda were triggered directly by S3.

The dispatcher deliberately processes inner S3 records sequentially. The outer SQS message is the AWS retry unit, so once one inner record fails the whole containing message must be retried. Continuing to execute later inner records would only increase the chance of duplicate side effects on that retry.

## S3ObjectEventHandler

`S3ObjectEventHandler` is ordinary application code:

```csharp
IS3ObjectEventHandler
```

It receives the synthetic `S3ObjectEvent` and an `S3RecordContext`, exactly like a direct S3 function would.

The sample also demonstrates context propagation:

```csharp
var sqsMessage = context.GetSqsMessage();
```

`S3RecordContext` is created from the outer `RecordContext` and inherits its property bag. This allows the inner S3 handler to inspect the containing SQS message without depending on scoped services from the outer SQS record.

## Scope model

Microsoft dependency injection scopes are not hierarchical. The inner S3 record scopes are independent scopes backed by the same root container; they are not child scopes that inherit scoped instances from the SQS record scope.

Conceptually:

```text
root container
├── SQS record scope
├── S3 record scope A
├── S3 record scope B
└── S3 record scope C
```

Outer delivery metadata is propagated explicitly through `RecordContext`, while scoped application dependencies remain isolated for every S3 record.

## Failure and retry boundary

AWS only knows about the SQS message delivered to Lambda. It does not know that the body contains several S3 records.

If `S3ObjectEventHandler` throws while processing any inner record, that exception propagates back through `S3EventDispatcher` to the SQS record pipeline. The SQS integration then reports the **containing SQS message** as failed in the partial-batch response.

Other SQS messages in the same Lambda invocation remain independent and can still succeed.

```text
SQS message A
└── S3 record failure
    → SQS message A failed

SQS message B
└── all S3 records succeed
    → SQS message B succeeds
```

The SQS message, not an individual nested S3 record, is therefore the acknowledgement and retry boundary.
