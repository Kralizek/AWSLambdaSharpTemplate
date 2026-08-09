# Choosing a Function Model

The library is organized around three semantic roots. Pick the root that describes what one Lambda invocation means.

## EventFunction

Use `EventFunction<TInput, THandler>` when the invocation receives an input, performs work, and has no application result to return.

Typical examples include generic one-way events and EventBridge events.

## RequestFunction

Use `RequestFunction<TInput, TOutput, THandler>` when the caller expects an application result.

Cognito trigger specializations build on this model because Cognito invokes the Lambda with an event and expects the event contract back after the handler has inspected or modified it.

## RecordFunction

Use record-oriented integrations when one Lambda invocation contains multiple independent records. The framework creates a record context and a dependency-injection scope for each record and lets the source-specific package define failure semantics.

SQS, SNS, DynamoDB Streams, Kinesis Streams, and S3 notifications use the record model, but their AWS semantics differ. Some sources support partial failure reporting while others fail the whole invocation.

## Prefer the source-specific specialization

When a dedicated package exists, use it instead of inheriting directly from `RecordFunction`. The specialization owns AWS envelope mapping, source metadata, failure responses, and the correct handler contract.
