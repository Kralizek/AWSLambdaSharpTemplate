# AWS Lambda Sharp Template

AWS Lambda Sharp Template provides .NET 10 runtime libraries and `dotnet new` templates for building Lambda functions around explicit programming models instead of wiring each invocation from scratch.

## Start here

- New to the library? Read [Getting Started](Getting-Started.md).
- Unsure which base type to use? Read [Choosing a Function Model](Choosing-a-Function-Model.md).
- Migrating an existing application? Read [Migrating from v5 to v6](Migrating-from-v5-to-v6.md).
- Composing nested record envelopes? Read [Record Processing](Record-Processing.md).
- Adding traces and metrics? Read [OpenTelemetry](OpenTelemetry.md).
- Publishing with Native AOT? Read [Native AOT](Native-AOT.md).

## AWS integrations

- [SQS](SQS.md)
- [SNS](SNS.md)
- [EventBridge](EventBridge.md)
- [DynamoDB Streams](DynamoDB-Streams.md)
- [Kinesis Streams](Kinesis-Streams.md)
- [S3](S3.md)
- [Cognito](Cognito.md)

## The three programming models

| Semantics | Root | Typical use |
| --- | --- | --- |
| Handle an input and complete | `EventFunction<TInput, THandler>` | One-way events such as EventBridge |
| Handle an input and return a result | `RequestFunction<TInput, TOutput, THandler>` | Request/response integrations such as Cognito |
| Process independent records in an envelope | `RecordFunction<...>` | SQS, SNS, DynamoDB Streams, Kinesis Streams, S3 notifications, and other record sources |

Source-specific packages specialize these roots while preserving a common model for dependency injection, configuration, logging, cancellation, contexts, and handler dispatch. `IRecordProcessor<TRecord, TRecordResult, TContext>` exposes the single-record execution primitive for advanced nested-record composition while source-specific functions retain ownership of envelope scheduling and AWS failure semantics.

## Install the templates

```bash
dotnet new install Kralizek.Lambda.Templates
```

Then list the available templates:

```bash
dotnet new list lambda-template
```

Generated projects target .NET 10 and use the standard AWS Lambda .NET tooling. Add `--otel` for OpenTelemetry instrumentation and `--aot` for Native AOT hosting and source-generated Lambda serialization. Source templates that support raw records can also compose these choices with `--raw`.
