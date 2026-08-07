# Kralizek.Lambda.Template

`Kralizek.Lambda.Template` provides a structured programming model for building AWS Lambda functions with .NET 10.

It brings together dependency injection, configuration, Lambda-compatible logging, cancellation, and explicit handler contracts while keeping the Lambda entry point small.

## Invocation models

Choose the semantic root that matches the Lambda invocation:

- `EventFunction<TInput, THandler>` for completion-only events.
- `RequestFunction<TInput, TOutput, THandler>` for request/response functions.
- `RecordFunction<...>` as the foundation for source-specific integrations that process envelopes of records.

### Event example

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

### Request example

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

## Payload decoding

Source-specific integrations can extract a raw text or binary payload and delegate contract deserialization through the lightweight decoder contracts from `Kralizek.Lambda.Template.Abstractions`.

This package provides System.Text.Json implementations for both representations:

```csharp
IStringPayloadDecoder<OrderCreated> stringDecoder =
    new JsonStringPayloadDecoder<OrderCreated>();

IBinaryPayloadDecoder<OrderCreated> binaryDecoder =
    new JsonBinaryPayloadDecoder<OrderCreated>();
```

Both decoders accept an optional `JsonSerializerOptions` instance when applications need custom naming policies or converters. Glue packages decide which decoder abstraction they consume and how the default implementation is registered.

Custom decoder packages can depend on `Kralizek.Lambda.Template.Abstractions` without referencing the full runtime package.

## Dependency injection

The primary handler is inferred from the function type and registered automatically as a scoped service.

Use `ConfigureServices` only for application dependencies:

```csharp
protected override void ConfigureServices(
    IServiceCollection services,
    IConfiguration configuration)
{
    services.AddScoped<IMyService, MyService>();
    base.ConfigureServices(services, configuration);
}
```

The built configuration is registered as `IConfiguration` and can be injected normally.

## Configuration and logging

Override the consumer hooks when needed:

```csharp
protected override void ConfigureConfiguration(IConfigurationBuilder configuration)
{
    base.ConfigureConfiguration(configuration);
}

protected override void ConfigureLogging(ILoggingBuilder logging)
{
    base.ConfigureLogging(logging);
}
```

Lambda-compatible logging is configured by default, so `ILogger<T>` works without additional bootstrap code.

## Contexts and cancellation

Handlers receive a strongly typed framework context and a cancellation token derived from the Lambda invocation's remaining time.

The standard contexts are:

- `EventContext`
- `RequestContext`
- `RecordContext`

The context and handler contracts live in `Kralizek.Lambda.Template.Abstractions`, which has no dependency on `Amazon.Lambda.Core`. Common Lambda invocation metadata is exposed as strongly typed CLR properties on `FunctionContext`.

This package provides `FunctionContextFactory`, which maps `ILambdaContext` into those source-neutral properties. The original AWS context is retained in the context property bag as an escape hatch and can be retrieved with `GetLambdaContext()` when an application needs runtime data not surfaced directly by the abstraction.

Source-specific integrations can derive richer context types and use `FunctionContextFactory.CreateMetadata(...)` and `CreateProperties(...)` to preserve the same base metadata and runtime escape hatch.

## Scopes

Event and request functions create one dependency-injection scope per invocation.

Record functions create one invocation scope and an independent scope for each record. This isolation is part of the record-processing model and lets source-specific integrations safely support sequential or bounded-parallel dispatch.

## Source-specific integrations

SNS, SQS, EventBridge, DynamoDB Streams, Kinesis, and other event-source integrations are layered on top of the core semantic roots. They are introduced in dedicated packages rather than adding source-specific behavior to this package.