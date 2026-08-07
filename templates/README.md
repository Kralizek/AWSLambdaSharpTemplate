# Kralizek.Lambda.Templates

Project templates for building AWS Lambda functions with `Kralizek.Lambda.Template`.

The package contains `dotnet new` templates that start from the semantic function model provided by the runtime library. The template package and runtime packages are versioned together, and generated projects reference the matching runtime package version.

## Install

```bash
dotnet new install Kralizek.Lambda.Templates
```

List the installed templates with:

```bash
dotnet new list lambda-template
```

## Available templates

| Template | Short name | Use when |
| --- | --- | --- |
| Event Function | `lambda-template-event` | The Lambda handles an input and does not return an application result. |
| Request Function | `lambda-template-request` | The Lambda handles an input and returns an application result. |
| SQS Function | `lambda-template-sqs` | The Lambda is triggered by SQS and processes each decoded message independently with partial-batch failure support. |

## Create a function

Create an event function:

```bash
dotnet new lambda-template-event --name MyEventFunction
```

Create a request function:

```bash
dotnet new lambda-template-request --name MyRequestFunction
```

Create an SQS function:

```bash
dotnet new lambda-template-sqs --name MySqsFunction
```

All templates support the AWS Lambda deployment settings exposed by the template package:

- `--profile` for the AWS credential profile used by the Lambda tools.
- `--region` for the AWS region.
- `--role` for the Lambda execution role.

For example:

```bash
dotnet new lambda-template-sqs \
  --name MySqsFunction \
  --profile my-profile \
  --region eu-north-1 \
  --role my-lambda-role
```

The generated project includes `aws-lambda-tools-defaults.json`, so it can be packaged and deployed with the standard `dotnet lambda` tooling.

## Programming model

An Event Function derives from `EventFunction<TInput, THandler>` and delegates application logic to an `IEventHandler<TInput>`:

```csharp
public class Function : EventFunction<string, StringEventHandler>
{
}
```

A Request Function derives from `RequestFunction<TInput, TOutput, THandler>` and delegates application logic to an `IRequestHandler<TInput, TOutput>`:

```csharp
public class Function : RequestFunction<string, string, ToUpperStringRequestHandler>
{
}
```

An SQS Function derives from `SqsFunction<TMessage, THandler>`. The framework decodes each SQS body to `TMessage`, creates an `SqsMessageContext`, and invokes the consumer's `ISqsMessageHandler<TMessage>`:

```csharp
public sealed class Function : SqsFunction<OrderCreated, OrderCreatedHandler>;
```

The default SQS payload decoder uses `System.Text.Json`. Applications can replace `IStringPayloadDecoder<TMessage>` through the normal dependency-injection customization hook when a different payload representation or source-generated JSON metadata is required.

## Package compatibility

`Kralizek.Lambda.Templates`, `Kralizek.Lambda.Template`, and source-specific packages such as `Kralizek.Lambda.Template.Sqs` use the same package version for a given release. A generated project therefore targets the exact runtime version that corresponds to the installed template package.

For the runtime programming model and API documentation, see the `Kralizek.Lambda.Template` package.