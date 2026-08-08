# AWS Lambda Sharp Template

Write AWS Lambda functions in C# with a programming model built around dependency injection, configuration, logging, and explicit handler contracts.

This repository is being refreshed for the next major release and targets .NET 10.

## Programming model

The library exposes three semantic roots:

- `EventFunction` for invocations that do not return a value.
- `RequestFunction` for request/response invocations.
- `RecordFunction` for event sources that deliver an envelope containing multiple records.

The function type defines the invocation model and the primary handler. The framework registers the handler as a scoped service and manages the appropriate dependency-injection scopes for each invocation.

### Event functions

```csharp
public sealed class Function : EventFunction<string, StringEventHandler>;

public sealed class StringEventHandler(ILogger<StringEventHandler> logger)
    : IEventHandler<string>
{
    public ValueTask HandleAsync(
        string input,
        EventContext context,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Received {Input}", input);
        return ValueTask.CompletedTask;
    }
}
```

### Request functions

```csharp
public sealed class Function
    : RequestFunction<string, string, ToUpperStringRequestHandler>;

public sealed class ToUpperStringRequestHandler
    : IRequestHandler<string, string>
{
    public ValueTask<string> HandleAsync(
        string input,
        RequestContext context,
        CancellationToken cancellationToken)
        => ValueTask.FromResult(input.ToUpperInvariant());
}
```

### Record functions

`RecordFunction` is the base for source-specific integrations such as SQS, SNS, DynamoDB Streams, and Kinesis. It preserves the association between each source record and its handler result, creates an isolated scope per record, and allows source-specific implementations to translate handler failures into the appropriate response model.

Source-specific integrations are being migrated in dedicated stacked pull requests and are intentionally not part of the active solution in this slice.

## Configuration, logging, and services

`LambdaFunction` provides three consumer customization hooks:

```csharp
protected override void ConfigureConfiguration(IConfigurationBuilder configuration)
{
    base.ConfigureConfiguration(configuration);
}

protected override void ConfigureLogging(ILoggingBuilder logging)
{
    base.ConfigureLogging(logging);
}

protected override void ConfigureServices(
    IServiceCollection services,
    IConfiguration configuration)
{
    base.ConfigureServices(services, configuration);
}
```

Lambda-compatible logging is configured by the framework. The primary handler is also registered by the framework, so consumers only register their application dependencies.

The built configuration is available through dependency injection as `IConfiguration`.

## Strongly typed contexts

Handlers receive framework contexts instead of working directly against `ILambdaContext`:

- `EventContext`
- `RequestContext`
- `RecordContext`

The full generic forms allow source-specific packages to provide richer context types without casts, while the compact request and event forms use the standard contexts automatically.

## Cancellation and scopes

Cancellation is derived from the remaining Lambda invocation time.

- Event and request functions create one scope per invocation.
- Record functions create one invocation scope plus an independently disposed scope for every record.
- Record processing is sequential by default, with bounded parallel processing available to source-specific implementations.

## Samples

The repository contains minimal samples for the two generic invocation models:

- [`samples/EventFunction`](samples/EventFunction)
- [`samples/RequestFunction`](samples/RequestFunction)

## Packages

The core programming model is published as `Kralizek.Lambda.Template`.

The repository also contains source-specific package projects for SNS and SQS. Those integrations are being redesigned on top of `RecordFunction` and will return to the active solution in their dedicated implementation slices.

## Templates

The `dotnet new` templates are maintained separately from the runtime programming model. The next template package is being aligned with the runtime package version and the new `EventFunction` / `RequestFunction` API.

## Building

The repository uses the XML solution format:

```bash
dotnet restore AWSLambdaSharpTemplate.slnx
dotnet build AWSLambdaSharpTemplate.slnx --configuration Release
dotnet test AWSLambdaSharpTemplate.slnx --configuration Release
```

## License

MIT