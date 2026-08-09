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

## Documentation

- [Getting Started](docs/Getting-Started.md)
- [Programming Model](docs/Programming-Model.md)
- [Payload Decoding](docs/Payload-Decoding.md)
- [Record Processing](docs/Record-Processing.md)
- [AWS integrations](docs/README.md#start-here)
- [Customization](docs/Customization.md)
- [Architecture](docs/Architecture.md)
- [Migrating from v5 to v6](docs/Migrating-from-v5-to-v6.md)
