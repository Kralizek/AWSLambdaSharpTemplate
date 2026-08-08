# AWS Lambda Sharp Template

AWS Lambda templates and runtime libraries for building .NET Lambda functions around explicit programming models instead of wiring every invocation from scratch.

The current programming model is organized around three semantic function roots:

- `EventFunction<TInput, THandler>` for one-way event handlers.
- `RequestFunction<TInput, TOutput, THandler>` for request/response handlers.
- `RecordFunction<...>` for integrations that process multiple independent records per invocation.

Source-specific packages build on those roots. `Kralizek.Lambda.Template.Sqs` provides SQS record processing with payload decoding and partial-batch failure handling, while `Kralizek.Lambda.Template.Sns` provides SNS notification processing with decoded or raw records and whole-invocation failure semantics.

## Packages

- `Kralizek.Lambda.Template.Abstractions` contains the source-neutral handler, context, and payload-decoder contracts.
- `Kralizek.Lambda.Template` contains the runtime implementation and generic function roots.
- `Kralizek.Lambda.Template.Sns` contains the SNS specialization.
- `Kralizek.Lambda.Template.Sqs` contains the SQS specialization.
- `Kralizek.Lambda.Templates` contains the `dotnet new` project templates.

## Generic event function

```csharp
public sealed class Function : EventFunction<string, StringEventHandler>;
```

## Generic request function

```csharp
public sealed class Function : RequestFunction<string, string, ToUpperStringRequestHandler>;
```

## SNS function

```csharp
public sealed class Function : SnsFunction<OrderCreated, OrderCreatedHandler>;
```

The SNS specialization decodes each SNS `Message` to `OrderCreated` and invokes `ISnsNotificationHandler<OrderCreated>` in an isolated per-record scope. SNS does not support partial-batch responses, so a failure in any notification fails the whole Lambda invocation. Raw SNS records and bounded-parallel processing are also supported.

## SQS function

```csharp
public sealed class Function : SqsFunction<OrderCreated, OrderCreatedHandler>;
```

The SQS specialization decodes each SQS message body to `OrderCreated`, invokes `ISqsMessageHandler<OrderCreated>` in an isolated per-record scope, and returns an AWS partial-batch response containing only failed message IDs.

## Project templates

Install the template package with:

```bash
dotnet new install Kralizek.Lambda.Templates
```

Available templates include:

```bash
dotnet new lambda-template-event --name MyEventFunction
dotnet new lambda-template-request --name MyRequestFunction
dotnet new lambda-template-sns --name MySnsFunction
dotnet new lambda-template-sqs --name MySqsFunction
```

The generated projects target .NET 10 and include `aws-lambda-tools-defaults.json` for use with the standard AWS Lambda .NET tooling.