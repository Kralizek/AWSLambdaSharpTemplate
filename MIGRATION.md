# Migrating from V5 to V6

V6 is a redesign of the programming model rather than an incremental API update. Existing V5 functions should be migrated intentionally instead of relying on compatibility shims.

## Target framework

V6 targets .NET 10. Update Lambda projects and deployment tooling accordingly before migrating application code.

## Choose the semantic function model

V6 organizes functions around three roots:

- `EventFunction` for one-way events;
- `RequestFunction` for request/response invocations;
- `RecordFunction` for batched record sources.

Source-specific packages build on those roots and should normally be preferred over the generic types when a supported AWS event source exists.

## Function and handler responsibilities

V6 separates the Lambda function declaration from application handler logic. The primary handler type is declared in the function generic arguments and resolved from dependency injection.

Application handlers receive a framework context plus a `CancellationToken`. The framework owns invocation scopes and handler activation.

Use `ConfigureServices`, `ConfigureConfiguration`, and `ConfigureLogging` for application customization. Do not register the primary handler manually unless you intentionally want to override its default scoped lifetime.

## Contexts

Handlers no longer need to depend directly on `ILambdaContext` for normal invocation metadata.

Use the strongly typed framework context supplied to the handler. Source integrations expose richer derived contexts for source-specific metadata.

When an AWS runtime object is intentionally not part of the stable abstraction, the original object is preserved in the context property bag and exposed through source-specific escape-hatch extensions.

## Payload serialization and decoding

The old source-specific serializer customization model has been replaced with payload decoders:

- `IStringPayloadDecoder<TPayload>` for string payloads such as SQS and SNS message bodies;
- `IBinaryPayloadDecoder<TPayload>` for binary payloads such as Kinesis records.

JSON decoders are registered by default and can be replaced through dependency injection. The decoder abstractions also support source-generated System.Text.Json metadata for Native AOT scenarios.

## SQS

Use `SqsFunction<TMessage, THandler>` for decoded message bodies or `SqsFunction<THandler>` for raw records.

SQS handlers return `SqsRecordResult` explicitly. Return `SqsRecordResult.Success` for successful records or `SqsRecordResult.Failed(...)` for records that should appear in the Lambda partial-batch response.

Bounded in-process parallelism remains available through the explicit `ParallelSqsFunction` variants.

Infrastructure must still configure the event source mapping appropriately for partial-batch responses.

## SNS

Use `SnsFunction<TNotification, THandler>` for decoded notifications or `SnsFunction<THandler>` for raw records.

SNS handlers return `SnsRecordResult.Completed`. SNS does not support Lambda partial-batch responses, so an exception still fails the entire invocation.

Bounded parallel processing is available only through the explicit `ParallelSnsFunction` variants.

## DynamoDB Streams

Use `DynamoDbStreamFunction<THandler>` from `Kralizek.Lambda.Template.DynamoDbStreams`.

Handlers receive a `DynamoDbStreamItem` plus `DynamoDbStreamRecordContext` and return `DynamoDbStreamRecordResult`.

Stream images use AWS `AttributeValue` dictionaries rather than pretending DynamoDB values are ordinary JSON.

Processing inside one Lambda invocation is sequential by design. Configure the Lambda event source mapping `ParallelizationFactor` when additional concurrency is needed so Lambda preserves stream ordering semantics.

## Kinesis Streams

Use the Kinesis Streams package for raw `KinesisEventRecord` handling or decoded binary payload handling.

Decoded handlers use `IBinaryPayloadDecoder<TPayload>` with JSON decoding registered by default. Handlers return `KinesisStreamRecordResult`.

Like DynamoDB Streams, processing within one invocation is sequential. Configure event-source parallelism in Lambda rather than adding in-process parallelism.

## EventBridge

Use `EventBridgeFunction<TDetail, THandler>`. The Lambda runtime serializer materializes `CloudWatchEvent<TDetail>` and the handler receives the typed detail while retaining access to the event envelope through the EventBridge context.

No additional payload decoder is involved.

## Cognito

V6 provides one strongly typed function base and handler interface for each supported Cognito user-pool trigger contract.

Pre-token-generation V1 and V2 are distinct runtime contracts. The project template exposes them through the `--version v1|v2` option.

## S3

V6 adds `S3Function<THandler>` for S3 event notifications and `S3BatchFunction<THandler>` for S3 Batch Operations schema 2.0.

The S3 integration exposes synthetic consumer-facing models while preserving access to the original AWS records through the source context.

S3 Batch schema 1.0 and S3 Object Lambda are not supported by the initial V6 implementation.

## Project templates

The V5 Empty/Rich template split has been removed. V6 provides semantic and source-specific templates instead.

Install the V6 template package and use `dotnet new list` to see the current template catalog. Generated projects reference the matching V6 runtime package version.

## Package layout

V6 includes a lightweight `Kralizek.Lambda.Template.Abstractions` package for consumer-facing contexts, handler contracts, record results, and decoder abstractions without bringing in the full runtime infrastructure.

Source-specific functionality lives in dedicated packages such as SQS, SNS, S3, EventBridge, Cognito, DynamoDB Streams, and Kinesis Streams.

## Recommended migration approach

Migrate one Lambda function at a time:

1. update the project to .NET 10 and V6 packages;
2. select the matching V6 semantic or source-specific function type;
3. move application logic into the corresponding handler contract;
4. replace direct `ILambdaContext` usage with the framework/source context where possible;
5. replace serializer hooks with payload decoders where applicable;
6. return the source-specific record result for batched sources;
7. review event-source mapping settings for partial-batch responses, retries, ordering, and parallelization;
8. build and test the function before moving to the next one.

See `CHANGELOG.md` for the complete V6 change summary and the package READMEs for source-specific behavior.