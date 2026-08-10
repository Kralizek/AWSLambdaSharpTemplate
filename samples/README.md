# Samples

The samples are intentionally small. Each one focuses on a specific part of the programming model so you can start from the problem you need to solve rather than from an AWS service list.

## Which sample should I use?

| If you want to demonstrate... | Start here | What it shows |
| --- | --- | --- |
| A Lambda that consumes an event and returns no application response | [EventFunction](EventFunction/) | `EventFunction<TInput, THandler>`, handler DI, logging, and `EventContext` |
| A Lambda with a typed request and response | [RequestFunction](RequestFunction/) | `RequestFunction<TInput, TOutput, THandler>` and `RequestContext` |
| JSON messages from SQS | [SqsFunction](SqsFunction/) | typed payload decoding, per-record handlers, and partial batch failures |
| SQS messages without decoding the body | [RawSqsFunction](RawSqsFunction/) | raw record handling when the AWS envelope is the application contract |
| JSON notifications from SNS | [SnsFunction](SnsFunction/) | typed payload decoding and SNS record handling |
| SNS notifications without decoding the message | [RawSnsFunction](RawSnsFunction/) | raw SNS record handling |
| A strongly typed EventBridge detail while retaining the full envelope | [EventBridgeFunction](EventBridgeFunction/) | `CloudWatchEvent<TDetail>` and EventBridge metadata |
| DynamoDB Streams | [DynamoDbStreamFunction](DynamoDbStreamFunction/) | stream item projections, source-specific results, partial failures, and sequential processing |
| Kinesis Streams with decoded binary payloads | [KinesisStreamFunction](KinesisStreamFunction/) | binary payload decoding, source-specific results, partial failures, and stream ordering |
| A Cognito trigger that mutates and returns the trigger event | [CognitoPreSignUpFunction](CognitoPreSignUpFunction/) | a concrete Cognito specialization using the pre-sign-up contract |

## Useful comparisons

### Generic event vs request/response

Start with [EventFunction](EventFunction/) if the caller does not consume an application response. Use [RequestFunction](RequestFunction/) when the Lambda invocation is explicitly request/response and your handler returns a value.

### Decoded vs raw messages

Compare [SqsFunction](SqsFunction/) with [RawSqsFunction](RawSqsFunction/), or [SnsFunction](SnsFunction/) with [RawSnsFunction](RawSnsFunction/). The decoded samples let the framework turn the message payload into an application type before invoking the handler. The raw samples keep the AWS record as the handler input when message attributes or the original envelope are central to the application logic.

### Queues vs streams

[SqsFunction](SqsFunction/) demonstrates record-oriented processing where bounded in-process parallelism can be an explicit specialization. [DynamoDbStreamFunction](DynamoDbStreamFunction/) and [KinesisStreamFunction](KinesisStreamFunction/) deliberately process records sequentially inside an invocation; use the Lambda event source mapping to control stream concurrency while preserving ordering semantics.

## Running the samples

The projects use project references to the packages in this repository and are part of the solution. Build all samples from the repository root with:

```bash
dotnet build
```

Each sample also contains `aws-lambda-tools-defaults.json` with example deployment settings. Treat those values as examples and replace profile, role, region, event-source configuration, and other infrastructure settings for your environment.
