# Kralizek.Lambda.Templates

Project templates for building AWS Lambda functions with `Kralizek.Lambda.Template`.

The package contains `dotnet new` templates that start from the semantic function model provided by the runtime library. The template package and runtime package are versioned together, and generated projects reference the matching runtime package version.

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

The package is designed to grow with additional AWS source-specific templates. New templates will be added to this catalog as their integrations become available, while the generic Event and Request templates remain the starting point when no source-specific behavior is required.

## Create a function

Create an event function:

```bash
dotnet new lambda-template-event --name MyEventFunction
```

Create a request function:

```bash
dotnet new lambda-template-request --name MyRequestFunction
```

Both templates support the AWS Lambda deployment settings exposed by the template package:

- `--profile` for the AWS credential profile used by the Lambda tools.
- `--region` for the AWS region.
- `--role` for the Lambda execution role.

For example:

```bash
dotnet new lambda-template-event \
  --name MyEventFunction \
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

Generated functions also expose the framework's consumer customization hooks for configuration, logging, and dependency injection. The primary handler is registered automatically by the framework.

## Package compatibility

`Kralizek.Lambda.Templates` and `Kralizek.Lambda.Template` use the same package version for a given release. A generated project therefore targets the exact runtime version that corresponds to the installed template package.

For the runtime programming model and API documentation, see the `Kralizek.Lambda.Template` package.