# AWS Lambda Sharp Template

AWS Lambda runtime libraries and `dotnet new` templates for .NET 10, built around explicit handler contracts, dependency injection, configuration, logging, cancellation, and source-specific AWS semantics.

## Programming model

The library has three semantic roots:

- `EventFunction<TInput, THandler>` for one-way events.
- `RequestFunction<TInput, TOutput, THandler>` for request/response functions.
- `RecordFunction<...>` for integrations that process multiple records per invocation.

AWS-specific packages specialize those roots for EventBridge, SQS, SNS, DynamoDB Streams, Kinesis Streams, S3, and Cognito.

Start with [Choosing a Function Model](docs/Choosing-a-Function-Model.md) or browse the [documentation](docs/README.md).

## Project templates

```bash
dotnet new install Kralizek.Lambda.Templates
dotnet new list lambda-template
```

See [Project Templates](docs/Project-Templates.md) for the supported templates and when to use each one.

The template options are independent where supported: `--raw` chooses the record/payload shape, `--aot` chooses executable hosting and source-generated Lambda serialization, and `--otel` adds Lambda invocation instrumentation.

## Package family

`Kralizek.Lambda.Templates` is the easiest entry point when starting a new Lambda function. The generated project references the runtime package appropriate for the selected template.

| Package | Stable | Latest | Downloads | Purpose |
| --- | --- | --- | --- | --- |
| [`Kralizek.Lambda.Templates`](https://www.nuget.org/packages/Kralizek.Lambda.Templates) | [![stable](https://img.shields.io/nuget/v/Kralizek.Lambda.Templates?label=stable)](https://www.nuget.org/packages/Kralizek.Lambda.Templates) | [![latest](https://img.shields.io/nuget/vpre/Kralizek.Lambda.Templates?label=latest)](https://www.nuget.org/packages/Kralizek.Lambda.Templates) | [![downloads](https://img.shields.io/nuget/dt/Kralizek.Lambda.Templates?label=downloads)](https://www.nuget.org/packages/Kralizek.Lambda.Templates) | `dotnet new` templates for the supported Lambda function models and AWS event sources. |
| [`Kralizek.Lambda.Template.Abstractions`](https://www.nuget.org/packages/Kralizek.Lambda.Template.Abstractions) | [![stable](https://img.shields.io/nuget/v/Kralizek.Lambda.Template.Abstractions?label=stable)](https://www.nuget.org/packages/Kralizek.Lambda.Template.Abstractions) | [![latest](https://img.shields.io/nuget/vpre/Kralizek.Lambda.Template.Abstractions?label=latest)](https://www.nuget.org/packages/Kralizek.Lambda.Template.Abstractions) | [![downloads](https://img.shields.io/nuget/dt/Kralizek.Lambda.Template.Abstractions?label=downloads)](https://www.nuget.org/packages/Kralizek.Lambda.Template.Abstractions) | Runtime-independent handler contracts, contexts, record results and payload-decoder abstractions. |
| [`Kralizek.Lambda.Template`](https://www.nuget.org/packages/Kralizek.Lambda.Template) | [![stable](https://img.shields.io/nuget/v/Kralizek.Lambda.Template?label=stable)](https://www.nuget.org/packages/Kralizek.Lambda.Template) | [![latest](https://img.shields.io/nuget/vpre/Kralizek.Lambda.Template?label=latest)](https://www.nuget.org/packages/Kralizek.Lambda.Template) | [![downloads](https://img.shields.io/nuget/dt/Kralizek.Lambda.Template?label=downloads)](https://www.nuget.org/packages/Kralizek.Lambda.Template) | Core Lambda runtime, including Event, Request and Record function models, lifecycle, dependency injection and framework telemetry. |
| [`Kralizek.Lambda.Template.Cognito`](https://www.nuget.org/packages/Kralizek.Lambda.Template.Cognito) | [![stable](https://img.shields.io/nuget/v/Kralizek.Lambda.Template.Cognito?label=stable)](https://www.nuget.org/packages/Kralizek.Lambda.Template.Cognito) | [![latest](https://img.shields.io/nuget/vpre/Kralizek.Lambda.Template.Cognito?label=latest)](https://www.nuget.org/packages/Kralizek.Lambda.Template.Cognito) | [![downloads](https://img.shields.io/nuget/dt/Kralizek.Lambda.Template.Cognito?label=downloads)](https://www.nuget.org/packages/Kralizek.Lambda.Template.Cognito) | Cognito User Pool trigger specializations and handler contracts. |
| [`Kralizek.Lambda.Template.DynamoDbStreams`](https://www.nuget.org/packages/Kralizek.Lambda.Template.DynamoDbStreams) | [![stable](https://img.shields.io/nuget/v/Kralizek.Lambda.Template.DynamoDbStreams?label=stable)](https://www.nuget.org/packages/Kralizek.Lambda.Template.DynamoDbStreams) | [![latest](https://img.shields.io/nuget/vpre/Kralizek.Lambda.Template.DynamoDbStreams?label=latest)](https://www.nuget.org/packages/Kralizek.Lambda.Template.DynamoDbStreams) | [![downloads](https://img.shields.io/nuget/dt/Kralizek.Lambda.Template.DynamoDbStreams?label=downloads)](https://www.nuget.org/packages/Kralizek.Lambda.Template.DynamoDbStreams) | DynamoDB Streams record processing and partial-batch response support. |
| [`Kralizek.Lambda.Template.EventBridge`](https://www.nuget.org/packages/Kralizek.Lambda.Template.EventBridge) | [![stable](https://img.shields.io/nuget/v/Kralizek.Lambda.Template.EventBridge?label=stable)](https://www.nuget.org/packages/Kralizek.Lambda.Template.EventBridge) | [![latest](https://img.shields.io/nuget/vpre/Kralizek.Lambda.Template.EventBridge?label=latest)](https://www.nuget.org/packages/Kralizek.Lambda.Template.EventBridge) | [![downloads](https://img.shields.io/nuget/dt/Kralizek.Lambda.Template.EventBridge?label=downloads)](https://www.nuget.org/packages/Kralizek.Lambda.Template.EventBridge) | EventBridge event-envelope specialization and source-specific context. |
| [`Kralizek.Lambda.Template.KinesisStreams`](https://www.nuget.org/packages/Kralizek.Lambda.Template.KinesisStreams) | [![stable](https://img.shields.io/nuget/v/Kralizek.Lambda.Template.KinesisStreams?label=stable)](https://www.nuget.org/packages/Kralizek.Lambda.Template.KinesisStreams) | [![latest](https://img.shields.io/nuget/vpre/Kralizek.Lambda.Template.KinesisStreams?label=latest)](https://www.nuget.org/packages/Kralizek.Lambda.Template.KinesisStreams) | [![downloads](https://img.shields.io/nuget/dt/Kralizek.Lambda.Template.KinesisStreams?label=downloads)](https://www.nuget.org/packages/Kralizek.Lambda.Template.KinesisStreams) | Kinesis Streams record processing, payload decoding and checkpoint/partial-batch behavior. |
| [`Kralizek.Lambda.Template.S3`](https://www.nuget.org/packages/Kralizek.Lambda.Template.S3) | [![stable](https://img.shields.io/nuget/v/Kralizek.Lambda.Template.S3?label=stable)](https://www.nuget.org/packages/Kralizek.Lambda.Template.S3) | [![latest](https://img.shields.io/nuget/vpre/Kralizek.Lambda.Template.S3?label=latest)](https://www.nuget.org/packages/Kralizek.Lambda.Template.S3) | [![downloads](https://img.shields.io/nuget/dt/Kralizek.Lambda.Template.S3?label=downloads)](https://www.nuget.org/packages/Kralizek.Lambda.Template.S3) | Native S3 event notifications and S3 Batch Operations. |
| [`Kralizek.Lambda.Template.Sns`](https://www.nuget.org/packages/Kralizek.Lambda.Template.Sns) | [![stable](https://img.shields.io/nuget/v/Kralizek.Lambda.Template.Sns?label=stable)](https://www.nuget.org/packages/Kralizek.Lambda.Template.Sns) | [![latest](https://img.shields.io/nuget/vpre/Kralizek.Lambda.Template.Sns?label=latest)](https://www.nuget.org/packages/Kralizek.Lambda.Template.Sns) | [![downloads](https://img.shields.io/nuget/dt/Kralizek.Lambda.Template.Sns?label=downloads)](https://www.nuget.org/packages/Kralizek.Lambda.Template.Sns) | SNS notification processing and payload decoding. |
| [`Kralizek.Lambda.Template.Sqs`](https://www.nuget.org/packages/Kralizek.Lambda.Template.Sqs) | [![stable](https://img.shields.io/nuget/v/Kralizek.Lambda.Template.Sqs?label=stable)](https://www.nuget.org/packages/Kralizek.Lambda.Template.Sqs) | [![latest](https://img.shields.io/nuget/vpre/Kralizek.Lambda.Template.Sqs?label=latest)](https://www.nuget.org/packages/Kralizek.Lambda.Template.Sqs) | [![downloads](https://img.shields.io/nuget/dt/Kralizek.Lambda.Template.Sqs?label=downloads)](https://www.nuget.org/packages/Kralizek.Lambda.Template.Sqs) | SQS message processing, payload decoding and partial-batch response support. |

The template package and all runtime packages use the same package version for a given release. For most applications, install `Kralizek.Lambda.Templates` and let the selected template choose the runtime package.

## Documentation

- [Getting Started](docs/Getting-Started.md)
- [Programming Model](docs/Programming-Model.md)
- [Payload Decoding](docs/Payload-Decoding.md)
- [Record Processing](docs/Record-Processing.md)
- [AWS integrations](docs/README.md#start-here)
- [Customization](docs/Customization.md)
- [Architecture](docs/Architecture.md)
- [Migrating from v5 to v6](docs/Migrating-from-v5-to-v6.md)
