# Migrating from v5 to v6

Version 6 is a programming-model redesign rather than a drop-in package update. This guide is intentionally mechanical: start from the v5 shape you have today and apply the matching changes below.

Migrate one Lambda at a time. Get the function building and behaving correctly before moving to the next one.

## Upgrade checklist

Apply these changes to every migrated function first:

1. Target .NET 10.
2. Update all `Kralizek.Lambda.*` runtime packages used by the function to the same v6 version.
3. Keep `Kralizek.Lambda.Template` for source-neutral Request/Event functions. Use the dedicated v6 package for AWS integrations such as SQS and SNS.
4. Put the handler type in the function base-class generic arguments. The primary handler is registered automatically.
5. Remove `RegisterHandler<THandler>(services)` and the v5 source-specific `Use...Handler(...)` registration calls.
6. Change handler methods to the v6 contract: framework context plus `CancellationToken`, normally returning `ValueTask`/`ValueTask<T>`.
7. Replace normal `ILambdaContext` usage with the v6 framework/source context. Use the AWS context escape hatch only when you actually need the original runtime object.
8. Update customization hooks:
   - `Configure(...)` becomes `ConfigureConfiguration(...)`;
   - `ConfigureLogging(ILoggingBuilder, IExecutionEnvironment)` becomes `ConfigureLogging(ILoggingBuilder)`;
   - `ConfigureServices(IServiceCollection, IExecutionEnvironment)` becomes `ConfigureServices(IServiceCollection, IConfiguration)`.
9. Preserve calls to the corresponding `base.Configure...(...)` methods unless you intentionally want to replace framework defaults.
10. Build and run the function's tests before changing another integration.

The sections below show the concrete v5-to-v6 transformation for the four most common v5 function shapes.

## Request/response functions

### v5

A v5 request/response function inherited from `RequestResponseFunction<TInput, TOutput>`, registered its handler manually, and implemented `IRequestResponseHandler<TInput, TOutput>`:

```csharp
public sealed class Function : RequestResponseFunction<string, string>
{
    protected override void ConfigureServices(
        IServiceCollection services,
        IExecutionEnvironment executionEnvironment)
        => RegisterHandler<ToUpperHandler>(services);
}

public sealed class ToUpperHandler : IRequestResponseHandler<string, string>
{
    public Task<string> HandleAsync(string input, ILambdaContext context)
        => Task.FromResult(input.ToUpperInvariant());
}
```

### v6

Use `RequestFunction<TInput, TOutput, THandler>` and `IRequestHandler<TInput, TOutput>`:

```csharp
public sealed class Function
    : RequestFunction<string, string, ToUpperHandler>;

public sealed class ToUpperHandler : IRequestHandler<string, string>
{
    public ValueTask<string> HandleAsync(
        string input,
        RequestContext context,
        CancellationToken cancellationToken)
        => ValueTask.FromResult(input.ToUpperInvariant());
}
```

Itemized changes:

- `RequestResponseFunction<TInput, TOutput>` -> `RequestFunction<TInput, TOutput, THandler>`.
- `IRequestResponseHandler<TInput, TOutput>` -> `IRequestHandler<TInput, TOutput>`.
- Move `THandler` into the function declaration.
- Delete `RegisterHandler<THandler>(services)`.
- `ILambdaContext` -> `RequestContext` for normal invocation metadata.
- Add the supplied `CancellationToken` to application calls that can observe cancellation.
- `Task<T>` -> `ValueTask<T>` in the handler contract. Async handlers can still use `async ValueTask<T>`.

### Full or Minimal host?

After migrating the handler contract, a source-neutral request function can choose either host without changing the handler:

```csharp
public sealed class Function
    : RequestFunction<string, string, ToUpperHandler>;
```

or:

```csharp
public sealed class Function
    : MinimalRequestFunction<string, string, ToUpperHandler>;
```

Use the full host by default. Consider Minimal when the function is latency/allocation-sensitive and does not need the full Request host's invocation telemetry infrastructure. See [Minimal Hosting](Minimal-Hosting.md) for the exact boundary.

## Event functions

### v5

A v5 one-way event function inherited from `EventFunction<TInput>`, registered the handler manually, and implemented `IEventHandler<TInput>`:

```csharp
public sealed class Function : EventFunction<OrderCreated>
{
    protected override void ConfigureServices(
        IServiceCollection services,
        IExecutionEnvironment executionEnvironment)
        => RegisterHandler<OrderCreatedHandler>(services);
}

public sealed class OrderCreatedHandler : IEventHandler<OrderCreated>
{
    public Task HandleAsync(OrderCreated input, ILambdaContext context)
        => Task.CompletedTask;
}
```

### v6

The function now names the handler in its generic arguments:

```csharp
public sealed class Function
    : EventFunction<OrderCreated, OrderCreatedHandler>;

public sealed class OrderCreatedHandler : IEventHandler<OrderCreated>
{
    public ValueTask HandleAsync(
        OrderCreated input,
        EventContext context,
        CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}
```

Itemized changes:

- `EventFunction<TInput>` -> `EventFunction<TInput, THandler>`.
- `IEventHandler<TInput>` remains the application concept, but its method signature changes.
- Move `THandler` into the function declaration.
- Delete `RegisterHandler<THandler>(services)`.
- `ILambdaContext` -> `EventContext` for normal invocation metadata.
- Add the supplied `CancellationToken`.
- `Task` -> `ValueTask` in the handler contract.

As with Request functions, a source-neutral Event function can use `MinimalEventFunction<TInput, THandler>` after the handler has been migrated if the Minimal hosting boundary fits the function.

## SNS functions

V5 modeled SNS as an Event function plus service-registration extensions. V6 makes SNS a first-class source-specific function model.

### v5

The typical v5 shape was an `EventFunction<SNSEvent>` plus `UseNotificationHandler<TNotification, THandler>()`:

```csharp
public sealed class Function : EventFunction<SNSEvent>
{
    protected override void ConfigureServices(
        IServiceCollection services,
        IExecutionEnvironment executionEnvironment)
    {
        services.UseNotificationHandler<OrderCreated, OrderCreatedHandler>();
    }
}
```

with an `INotificationHandler<TNotification>` application handler.

### v6: decoded notification

Use `Kralizek.Lambda.Template.Sns` and declare the notification type and handler directly:

```csharp
public sealed class Function
    : SnsFunction<OrderCreated, OrderCreatedHandler>;

public sealed class OrderCreatedHandler
    : ISnsNotificationHandler<OrderCreated>
{
    public ValueTask<SnsRecordResult> HandleAsync(
        OrderCreated notification,
        SnsNotificationContext context,
        CancellationToken cancellationToken)
        => ValueTask.FromResult(SnsRecordResult.Completed);
}
```

Itemized changes:

- `EventFunction<SNSEvent>` -> `SnsFunction<TNotification, THandler>`.
- `INotificationHandler<TNotification>` -> `ISnsNotificationHandler<TNotification>`.
- Delete `UseNotificationHandler<TNotification, THandler>()`.
- The SNS `Message` is decoded for you through `IStringPayloadDecoder<TNotification>`.
- Return `SnsRecordResult.Completed` when processing completes normally.
- Use `SnsNotificationContext` for source metadata.
- If you need the original AWS `SNSEvent.SNSRecord`, call `context.GetSnsRecord()`.

### v6: raw SNS record

If the v5 code depended on the entire SNS record rather than only the decoded `Message`, migrate to the raw variant instead of decoding and rebuilding the envelope yourself:

```csharp
public sealed class Function
    : SnsFunction<RawSnsHandler>;

public sealed class RawSnsHandler : ISnsRecordHandler
{
    public ValueTask<SnsRecordResult> HandleAsync(
        SNSEvent.SNSRecord record,
        SnsRecordContext context,
        CancellationToken cancellationToken)
        => ValueTask.FromResult(SnsRecordResult.Completed);
}
```

### SNS parallel execution

V5 enabled parallel processing through:

```csharp
services
    .UseNotificationHandler<OrderCreated, OrderCreatedHandler>()
    .WithParallelExecution(maxDegreeOfParallelism: 4);
```

In v6, parallel processing is explicit in the function type. Use the corresponding `ParallelSnsFunction<...>` variant and configure its supported degree of parallelism rather than chaining a registration extension.

Sequential processing is the default.

### SNS custom serialization

V5 custom serializers implemented `INotificationSerializer` and were registered with `UseCustomNotificationSerializer<TSerializer>()`.

In v6, replace that serializer with an `IStringPayloadDecoder<TNotification>` implementation and register it through dependency injection. JSON decoding is already registered by default.

SNS still has no Lambda partial-batch response. If decoding or handling a record throws, the invocation fails and AWS applies normal SNS/Lambda retry behavior.

## SQS functions

V5 modeled SQS through generic Event/RequestResponse infrastructure plus SQS registration helpers. V6 makes record processing, decoding, record scopes, and partial-batch results explicit.

### v5

A typical typed v5 function registered a queue-message handler:

```csharp
public sealed class Function : EventFunction<SQSEvent>
{
    protected override void ConfigureServices(
        IServiceCollection services,
        IExecutionEnvironment executionEnvironment)
    {
        services.UseQueueMessageHandler<OrderCreated, OrderCreatedHandler>();
    }
}
```

with an `IMessageHandler<TMessage>` application handler.

Some v5 functions instead used `RequestResponseFunction<SQSEvent, SQSBatchResponse>` directly and built `BatchItemFailures` themselves.

### v6: decoded message

Use `Kralizek.Lambda.Template.Sqs` and declare the body type and handler directly:

```csharp
public sealed class Function
    : SqsFunction<OrderCreated, OrderCreatedHandler>;

public sealed class OrderCreatedHandler
    : ISqsMessageHandler<OrderCreated>
{
    public ValueTask<SqsRecordResult> HandleAsync(
        OrderCreated message,
        SqsMessageContext context,
        CancellationToken cancellationToken)
        => ValueTask.FromResult(SqsRecordResult.Success);
}
```

Itemized changes:

- Generic `EventFunction<SQSEvent>` / hand-written `RequestResponseFunction<SQSEvent, SQSBatchResponse>` -> `SqsFunction<TMessage, THandler>`.
- `IMessageHandler<TMessage>` -> `ISqsMessageHandler<TMessage>`.
- Delete `UseQueueMessageHandler<TMessage, THandler>()` and `RegisterHandler<THandler>()`.
- The SQS body is decoded through `IStringPayloadDecoder<TMessage>`.
- Return `SqsRecordResult.Success` for a successful record.
- Return `SqsRecordResult.Failed(reason)` for a record that must appear in `batchItemFailures`.
- Use `SqsMessageContext` for source metadata.
- If you need the original `SQSEvent.SQSMessage`, call `context.GetSqsMessage()`.

### v6: raw SQS record

If your v5 handler worked with the original record, use the raw variant:

```csharp
public sealed class Function
    : SqsFunction<RawSqsHandler>;

public sealed class RawSqsHandler : ISqsRecordHandler
{
    public ValueTask<SqsRecordResult> HandleAsync(
        SQSEvent.SQSMessage record,
        SqsRecordContext context,
        CancellationToken cancellationToken)
        => ValueTask.FromResult(SqsRecordResult.Success);
}
```

### Replace hand-built `SQSBatchResponse`

If a v5 function manually iterated the batch and constructed `SQSBatchResponse`, move the per-record behavior into the v6 handler and return a record result instead.

v5:

```csharp
foreach (var record in input.Records)
{
    try
    {
        await ProcessAsync(record);
    }
    catch (Exception)
    {
        failures.Add(new SQSBatchResponse.BatchItemFailure
        {
            ItemIdentifier = record.MessageId
        });
    }
}

return new SQSBatchResponse
{
    BatchItemFailures = failures
};
```

v6:

```csharp
public async ValueTask<SqsRecordResult> HandleAsync(
    OrderCreated message,
    SqsMessageContext context,
    CancellationToken cancellationToken)
{
    var processed = await ProcessAsync(message, cancellationToken);

    return processed
        ? SqsRecordResult.Success
        : SqsRecordResult.Failed("Message could not be processed.");
}
```

The SQS host owns the final `SQSBatchResponse` aggregation.

### SQS parallel execution

V5 enabled in-process parallelism with:

```csharp
services
    .UseQueueMessageHandler<OrderCreated, OrderCreatedHandler>()
    .WithParallelExecution(maxDegreeOfParallelism: 4);
```

In v6, choose the corresponding `ParallelSqsFunction<...>` variant. Sequential processing is the default.

### SQS custom serialization

V5 custom serializers implemented `IMessageSerializer` and were registered with `UseCustomMessageSerializer<TSerializer>()`.

In v6, replace that serializer with `IStringPayloadDecoder<TMessage>` and register the decoder through dependency injection. JSON decoding is registered by default.

### Infrastructure check: partial batch response

The v6 code can produce partial-batch failures, but the Lambda event source mapping must still have `ReportBatchItemFailures` enabled. Do not treat this as an application-code-only migration.

Also review the event source mapping's batch size, retry/DLQ behavior, concurrency settings, and visibility timeout while validating the migrated function.

## Configuration, logging, and services

A common v5 function combined handler registration with application customization:

```csharp
protected override void Configure(IConfigurationBuilder configuration)
{
    configuration.AddEnvironmentVariables();
}

protected override void ConfigureLogging(
    ILoggingBuilder logging,
    IExecutionEnvironment executionEnvironment)
{
    // ...
}

protected override void ConfigureServices(
    IServiceCollection services,
    IExecutionEnvironment executionEnvironment)
{
    services.AddScoped<IMyService, MyService>();
    RegisterHandler<MyHandler>(services);
}
```

The equivalent v6 customization is:

```csharp
protected override void ConfigureConfiguration(
    IConfigurationBuilder configuration)
{
    configuration.AddEnvironmentVariables();
    base.ConfigureConfiguration(configuration);
}

protected override void ConfigureLogging(ILoggingBuilder logging)
{
    // Add or customize logging here.
    base.ConfigureLogging(logging);
}

protected override void ConfigureServices(
    IServiceCollection services,
    IConfiguration configuration)
{
    services.AddScoped<IMyService, MyService>();
    base.ConfigureServices(services, configuration);
}
```

Do not register the primary handler unless you intentionally want to override its default scoped lifetime.

Lambda-compatible logging is configured by the framework, so existing `ILogger<T>` dependencies normally require less bootstrap code than in v5.

## `ILambdaContext` migration

Do not mechanically inject the original `ILambdaContext` into every v6 handler just because the v5 handler received it.

Use the supplied v6 context first:

- `RequestContext` for Request functions;
- `EventContext` for Event functions;
- `SqsMessageContext` / `SqsRecordContext` for SQS;
- `SnsNotificationContext` / `SnsRecordContext` for SNS.

These contexts expose the stable invocation/source metadata expected by application code. Source-specific contexts also preserve the original AWS records through escape-hatch extensions when needed.

If application code intentionally requires something that is only available from the AWS runtime context, use the framework's AWS-context escape hatch rather than making the AWS object the default application abstraction.

## Handler lifetime and scopes

V5 users often registered handlers themselves, which made the lifetime part of application setup.

In v6:

- the primary handler is registered automatically as scoped;
- Request/Event hosts create one DI scope per invocation;
- SQS/SNS create one DI scope per record;
- source-specific parallel variants may therefore execute multiple independent record scopes concurrently.

Review dependencies that were accidentally relying on a singleton or invocation-wide lifetime when migrating SQS/SNS handlers.

## Payload decoding

V5 had source-specific serialization hooks:

- `INotificationSerializer` for SNS;
- `IMessageSerializer` for SQS.

V6 uses the common payload-decoder abstraction:

```csharp
public sealed class OrderCreatedDecoder
    : IStringPayloadDecoder<OrderCreated>
{
    public ValueTask<OrderCreated> DecodeAsync(
        string payload,
        CancellationToken cancellationToken)
    {
        // Decode the payload.
    }
}
```

Register your decoder through DI. The v6 source package supplies JSON decoding by default, including a path for source-generated System.Text.Json metadata in Native AOT functions.

## Package and template changes

The v5 Empty/Rich template split is gone. V6 templates are organized by semantic function model and AWS event source.

Install the v6 templates and inspect the current catalog:

```bash
dotnet new install Kralizek.Lambda.Templates

dotnet new list lambda-template
```

For an existing project, using a freshly generated v6 project of the same function type as a reference is often the fastest way to compare package references, serializer/bootstrap code, and deployment settings.

## Migration verification checklist

Before considering one function migrated, verify all of the following:

- [ ] project targets .NET 10;
- [ ] all Kralizek runtime packages use the same v6 version;
- [ ] function base class matches its invocation semantics/source;
- [ ] primary handler is declared in the function type;
- [ ] old handler registration calls are removed;
- [ ] handler implements the v6 contract and accepts cancellation;
- [ ] direct `ILambdaContext` usage has been reviewed rather than blindly carried over;
- [ ] configuration/logging/service overrides use the v6 signatures;
- [ ] custom SNS/SQS serializers have become payload decoders;
- [ ] SQS handlers return explicit success/failure results;
- [ ] SQS event source mapping has `ReportBatchItemFailures` configured when partial-batch behavior is expected;
- [ ] previous parallel-processing behavior has been deliberately restored, if needed;
- [ ] DI lifetime assumptions still hold with v6 invocation/record scopes;
- [ ] unit/integration tests exercise successful and failing records;
- [ ] deployment package and Lambda runtime are .NET 10-compatible.

## Performance and choosing Minimal

V6 has two hosting choices for source-neutral Request/Event functions. The full host provides the complete v6 invocation infrastructure. Minimal runs the same handler/context programming model through a shorter path when that infrastructure is not needed.

The current release benchmark snapshot measured the following Request workload on its GitHub-hosted runner:

| Model | Mean | Allocated |
| --- | ---: | ---: |
| Raw AWS SDK | 32.2 ns | 128 B |
| V5 | 196.7 ns | 352 B |
| V6 Minimal | 299.6 ns | 584 B |
| V6 full | 443.0 ns | 712 B |

A nested SQS -> SNS -> S3 workload measured:

| Model | Mean | Allocated |
| --- | ---: | ---: |
| Raw AWS SDK | 5.60 us | 4,168 B |
| V5 | 6.01 us | 4,280 B |
| V6 Minimal comparison | 5.88 us | 4,624 B |
| V6 full/source-specific | 7.93 us | 6,088 B |

The Minimal contender in record-oriented benchmarks is a comparison fixture, not a `MinimalSqsFunction` or `MinimalSnsFunction`. It moves AWS-specific iteration, decoding, and failure translation back into application code so the benchmark can isolate hosting overhead.

Treat these measurements as architectural comparisons, not universal throughput ratios. Benchmark the actual workload when latency or allocations matter.

See [the benchmark documentation](../benchmarks/README.md) for methodology and release benchmark history.

## Related documentation

- [Request Functions](Request-Functions.md)
- [Event Functions](Event-Functions.md)
- [SQS](SQS.md)
- [SNS](SNS.md)
- [Function Contexts](Function-Contexts.md)
- [Payload Decoding](Payload-Decoding.md)
- [Record Processing](Record-Processing.md)
- [Customization](Customization.md)
- [Minimal Hosting](Minimal-Hosting.md)
