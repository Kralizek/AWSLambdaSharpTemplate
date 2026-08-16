# Samples

The samples are intentionally small. Each one focuses on a specific part of the programming model so you can start from the problem you need to solve rather than from an AWS service list.

Each sample README shows the expected Lambda input shape and how that input maps to the framework. AWS-specific samples also include a minimal Terraform sketch when the event-source topology is part of the concept. Those snippets intentionally omit general Lambda packaging, IAM, and unrelated infrastructure unless it is necessary to explain the integration.

## Which sample should I use?

| If you want to demonstrate... | Start here | What it shows |
| --- | --- | --- |
| A Lambda that consumes an event and returns no application response | [EventFunction](EventFunction/) | `EventFunction<TInput, THandler>`, handler DI, logging, and `EventContext` |
| A Lambda with a typed request and response | [RequestFunction](RequestFunction/) | `RequestFunction<TInput, TOutput, THandler>` and `RequestContext` |
| Native AOT hosting for a typed SQS function | [NativeAotSqsFunction](NativeAotSqsFunction/) | executable Lambda bootstrap, source-generated boundary metadata, and an AOT-safe nested JSON decoder |
| Native AOT for S3 events delivered through an SNS envelope and SQS | [NativeAotSqsSnsS3Function](NativeAotSqsSnsS3Function/) | raw SQS orchestration with source-generated metadata for both nested JSON payloads |
| OpenTelemetry instrumentation for a request/response Lambda | [OpenTelemetryRequestFunction](OpenTelemetryRequestFunction/) | wrapping the inherited request handler with `AWSLambdaWrapper.TraceAsync` and exporting the invocation span |
| OpenTelemetry instrumentation for an event Lambda | [OpenTelemetryEventFunction](OpenTelemetryEventFunction/) | the same standard AWS Lambda OpenTelemetry wrapper pattern applied to `EventFunction` |
| OpenTelemetry instrumentation for SQS record processing | [OpenTelemetrySqsFunction](OpenTelemetrySqsFunction/) | wrapping a typed SQS record function while preserving `SQSBatchResponse` and partial batch failures |
| JSON messages from SQS | [SqsFunction](SqsFunction/) | typed payload decoding, `IRecordProcessor`-backed per-record scopes, and partial batch failures |
| SQS messages without decoding the body | [RawSqsFunction](RawSqsFunction/) | raw record handling when the AWS envelope is the application contract |
| S3 notifications delivered through SNS → SQS with raw message delivery | [SqsRawSnsS3Function](SqsRawSnsS3Function/) | typed `S3Event` payload decoding when the SQS body contains the S3 event directly |
| S3 notifications delivered through SNS → SQS with the SNS envelope preserved | [SqsSnsS3Function](SqsSnsS3Function/) | raw SQS orchestration that decodes the SNS envelope and nested S3 event before delegating S3 records |
| JSON notifications from SNS | [SnsFunction](SnsFunction/) | typed payload decoding and SNS record handling |
| SNS notifications without decoding the message | [RawSnsFunction](RawSnsFunction/) | raw SNS record handling |
| A strongly typed EventBridge detail while retaining the full envelope | [EventBridgeFunction](EventBridgeFunction/) | `CloudWatchEvent<TDetail>` and EventBridge metadata |
| DynamoDB Streams | [DynamoDbStreamFunction](DynamoDbStreamFunction/) | stream item projections, source-specific results, partial failures, and sequential processing |
| Kinesis Streams with decoded binary payloads | [KinesisStreamFunction](KinesisStreamFunction/) | binary payload decoding, source-specific results, partial failures, and stream ordering |
| A Cognito trigger that mutates and returns the trigger event | [CognitoPreSignUpFunction](CognitoPreSignUpFunction/) | a concrete Cognito specialization using the pre-sign-up contract |

## Useful comparisons

### Generic event vs request/response

Start with [EventFunction](EventFunction/) if the caller does not consume an application response. Use [RequestFunction](RequestFunction/) when the Lambda invocation is explicitly request/response and your handler returns a value.

### OpenTelemetry across function shapes

Compare [OpenTelemetryRequestFunction](OpenTelemetryRequestFunction/), [OpenTelemetryEventFunction](OpenTelemetryEventFunction/), and [OpenTelemetrySqsFunction](OpenTelemetrySqsFunction/). All three use the standard `OpenTelemetry.Instrumentation.AWSLambda` wrapper around the inherited `FunctionHandlerAsync`; only the Lambda handler signature changes. The framework lifecycle remains unchanged underneath the wrapper, including SQS partial-batch-response behavior.

### Native AOT vs regular SQS hosting

Compare [NativeAotSqsFunction](NativeAotSqsFunction/) with [SqsFunction](SqsFunction/). The handler and framework programming model remain the same; the Native AOT sample adds the executable runtime bootstrap, source-generated Lambda boundary metadata, and generated `JsonTypeInfo<T>` metadata for the nested application payload decoder.

For a deeper nested payload, compare [NativeAotSqsSnsS3Function](NativeAotSqsSnsS3Function/) with [SqsSnsS3Function](SqsSnsS3Function/). Both use the same raw SQS topology handler and S3 record-processing model. Native AOT supplies generated metadata to the `SnsEnvelope` and `S3Event` decoders in addition to the Lambda `SQSEvent`/`SQSBatchResponse` boundary.

### Decoded vs raw messages

Compare [SqsFunction](SqsFunction/) with [RawSqsFunction](RawSqsFunction/), or [SnsFunction](SnsFunction/) with [RawSnsFunction](RawSnsFunction/). The decoded samples let the framework turn the message payload into an application type before invoking the handler. The raw samples keep the AWS record as the handler input when message attributes or the original envelope are central to the application logic.

### Nested S3 notifications through SNS and SQS

Compare [SqsRawSnsS3Function](SqsRawSnsS3Function/) with [SqsSnsS3Function](SqsSnsS3Function/). The AWS topology is the same; the SNS subscription's `raw_message_delivery` setting changes the payload shape from `SQS body → S3Event` to `SQS body → SNS envelope → Message → S3Event`.

When the SQS body is directly an `S3Event`, the typed SQS programming model is a natural fit. When the SNS envelope is preserved, the raw SQS handler owns both nested decoding stages as one topology-specific concern. Once either path reaches S3 records, both samples use the public `IRecordProcessor` to reuse the framework's normal S3 per-record scope, telemetry, handler activation, and context adaptation. The processor handles one inner record at a time; the outer SQS function remains responsible for iteration-level failure translation and the AWS retry boundary.

### Queues vs streams

[SqsFunction](SqsFunction/) demonstrates record-oriented processing where bounded in-process parallelism can be an explicit specialization. [DynamoDbStreamFunction](DynamoDbStreamFunction/) and [KinesisStreamFunction](KinesisStreamFunction/) deliberately process records sequentially inside an invocation; use the Lambda event source mapping to control stream concurrency while preserving ordering semantics.

## Running the samples

The projects use project references to the packages in this repository and are part of the solution. Build all samples from the repository root with:

```bash
dotnet build
```

Native AOT projects should additionally be published for the Lambda target runtime, for example:

```bash
dotnet publish samples/NativeAotSqsFunction -c Release -r linux-x64 --self-contained true --warnaserror
dotnet publish samples/NativeAotSqsSnsS3Function -c Release -r linux-x64 --self-contained true --warnaserror
```

Deployment-oriented samples may also contain `aws-lambda-tools-defaults.json` with example deployment settings. Treat those values as examples and replace profile, role, region, event-source configuration, and other infrastructure settings for your environment.
