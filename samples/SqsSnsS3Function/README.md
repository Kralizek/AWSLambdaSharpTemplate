# SQS → SNS envelope → S3

This sample demonstrates processing S3 object notifications that reach Lambda through an SNS subscription to SQS using the **standard SNS JSON envelope**.

The Lambda event source is SQS. Unlike the raw-delivery sample, each SQS message body contains an SNS envelope, and the S3 event is serialized inside the envelope's `Message` property.

```text
S3 notification
    ↓
SNS topic
    ↓ standard SNS delivery
SQS message body = SNS envelope JSON
    ↓
Lambda
    ↓
SqsFunction<SnsEnvelope, SnsEnvelopedS3DeliveryHandler>
    ↓
decode SnsEnvelope.Message → S3Event
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

This sample differs from `SqsRawSnsS3Function` in one configuration detail only: the SNS subscription does **not** use Raw Message Delivery. That means the SQS message body contains the standard SNS envelope, and the S3 event is nested inside the envelope's `Message` property.

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

A complete deployment also needs an SNS topic policy that allows S3 to publish, an SQS queue policy that allows the SNS topic to call `sqs:SendMessage`, and the usual Lambda packaging and permission resources. Those details are omitted so the sample stays focused on the event-processing topology.

## Example Lambda input

Because `raw_message_delivery = false`, the function receives an `SQSEvent` whose `Records[*].body` contains the SNS envelope rather than the `S3Event` directly.

A trimmed example looks like this:

```json
{
  "Records": [
    {
      "messageId": "7f8c3ad7-3c2a-4dc6-95f4-bb6b0c6f0b52",
      "receiptHandle": "...",
      "body": "{\"Type\":\"Notification\",\"MessageId\":\"2d402878-82d2-4c23-b5b9-6632f9f4fa71\",\"TopicArn\":\"arn:aws:sns:eu-north-1:123456789012:s3-events\",\"Message\":\"{\\\"Records\\\":[{\\\"eventSource\\\":\\\"aws:s3\\\",\\\"eventName\\\":\\\"ObjectCreated:Put\\\",\\\"s3\\\":{\\\"bucket\\\":{\\\"name\\\":\\\"example-uploads\\\"},\\\"object\\\":{\\\"key\\\":\\\"documents/report.pdf\\\",\\\"size\\\":12345}}}]}\"}",
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
    └── Body: SNS envelope
        └── Message: S3Event
            └── Records[]
                └── S3 record
```

That outer shape maps to:

```csharp
SqsFunction<SnsEnvelope, SnsEnvelopedS3DeliveryHandler>
```

The SQS integration decodes the body into `SnsEnvelope`. The handler then performs the second decoding step from `SnsEnvelope.Message` to `S3Event`.

## Function

`Function` derives from:

```csharp
SqsFunction<SnsEnvelope, SnsEnvelopedS3DeliveryHandler>
```

The SQS integration first decodes each `SQSMessage.Body` into the local `SnsEnvelope` model. For this sample only the `Message` property is needed, so the model intentionally contains only that property rather than reproducing the complete SNS envelope.

`ConfigureServices` registers the nested S3 processing pieces:

```csharp
services.AddS3ObjectEventProcessing<S3ObjectEventHandler>();
services.TryAddScoped<S3EventDispatcher>();
services.TryAddSingleton<IStringPayloadDecoder<S3Event>, JsonStringPayloadDecoder<S3Event>>();
```

`AddS3ObjectEventProcessing<THandler>()` exposes the same S3 record-processing composition used by direct S3 functions. The explicit `IStringPayloadDecoder<S3Event>` registration handles the second decoding layer: SNS stores the original S3 event as a string in `Message`.

## SnsEnvelope

`SnsEnvelope` represents the outer SNS payload stored in the SQS body:

```csharp
public sealed record SnsEnvelope
{
    public string Message { get; init; } = string.Empty;
}
```

It is deliberately minimal. The sample is concerned with nested record composition, not with modeling every SNS metadata property. Applications that need additional SNS metadata can extend this model accordingly.

## SnsEnvelopedS3DeliveryHandler

`SnsEnvelopedS3DeliveryHandler` handles one **outer SQS record** after the SQS body has been decoded into `SnsEnvelope`.

Its first responsibility is to decode the SNS `Message` value into the actual S3 event:

```csharp
var s3Event = await _decoder.DecodeAsync(message.Message, cancellationToken);
```

It then delegates that S3 event to `S3EventDispatcher`.

Only after all inner S3 records complete successfully does the handler return `SqsRecordResult.Success` for the containing SQS message.

This extra decoder is the main difference from `SqsRawSnsS3Function`: with SNS Raw Message Delivery enabled, the SQS integration can decode the body directly as `S3Event` and this intermediate step disappears.

## S3EventDispatcher

An S3 event may contain several object-notification records. `S3EventDispatcher` expands the inner collection and processes each record through:

```csharp
IRecordProcessor<
    S3Event.S3EventNotificationRecord,
    S3RecordResult,
    RecordContext>
```

The record processor preserves the framework's **one DI scope per record** invariant. Without it, a nested pipeline would either have to share the outer SQS record scope for all S3 records or duplicate the framework's scope creation and S3 handler activation logic.

Each `ProcessAsync` call creates and disposes an independent S3 record scope and invokes the same S3 adapter/application-handler path used by a direct `S3Function<THandler>`.

Inner records are processed sequentially on purpose. If one inner S3 record fails, AWS can only retry the outer SQS message. Processing later inner records after that failure would create additional side effects that may be repeated when the message is retried.

## S3ObjectEventHandler

`S3ObjectEventHandler` remains a normal:

```csharp
IS3ObjectEventHandler
```

The application handler does not need to know how many transport envelopes were crossed before the event reached it. It receives an `S3ObjectEvent` plus `S3RecordContext`, just as it would for a direct S3-triggered Lambda.

The context also retains the outer SQS metadata:

```csharp
var sqsMessage = context.GetSqsMessage();
```

The S3 adapter creates its context from the outer `RecordContext` and copies the parent's property bag. This explicit context propagation lets the inner handler inspect the containing SQS message without coupling nested processing to DI scope inheritance.

## Scope model

Microsoft dependency injection does not provide hierarchical scopes. The SQS record scope and each nested S3 record scope are independent scopes backed by the same root container.

```text
root container
├── SQS record scope
├── S3 record scope A
├── S3 record scope B
└── S3 record scope C
```

Scoped dependencies do not flow from the SQS handler into the S3 handlers. Delivery metadata that must survive the transition is propagated through `RecordContext` instead.

This is intentional: an S3 application handler gets the same scoped-lifetime behavior regardless of whether S3 invokes Lambda directly or its event arrives nested inside SNS and SQS.

## Failure and retry boundary

There are three logical layers in the payload, but only one AWS event-source acknowledgement boundary for this Lambda: SQS.

```text
SQS message
└── SNS envelope
    └── S3Event
        ├── S3 record A
        ├── S3 record B
        └── S3 record C
```

If processing any inner S3 record throws, the exception propagates through the dispatcher and the outer SQS handler. The SQS integration then puts the **containing SQS message** in `SQSBatchResponse.batchItemFailures`.

An individual nested S3 record cannot be retried independently because AWS Lambda received only the SQS record. Unrelated SQS messages in the same invocation remain independent and can still be acknowledged successfully.

## Why this sample exists separately from the raw-delivery sample

Both samples intentionally duplicate the small dispatcher and S3 application handler so each project can be read on its own.

The difference between them is the transport shape before S3 record processing begins:

```text
Raw delivery:      SQS body → S3Event
Standard delivery: SQS body → SNS envelope → Message → S3Event
```

From `S3EventDispatcher` onward, both use the same `IRecordProcessor` composition model and the same `IS3ObjectEventHandler` application contract.
