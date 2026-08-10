# AWS Lambda Sharp Template

AWS Lambda Sharp Template provides .NET 10 runtime libraries and `dotnet new` templates for building AWS Lambda functions around a small, dependency-injection-friendly programming model.

The library separates Lambda functions into three semantic models:

- **Event functions** consume an input and return no application response.
- **Request functions** consume an input and return an output.
- **Record functions** process records from AWS envelopes such as SQS, SNS, DynamoDB Streams, Kinesis Streams, and S3 notifications.

Source-specific packages build on those models while keeping AWS-specific envelope, retry, ordering, and failure semantics explicit.

## Documentation

- [Documentation](docs/README.md)
- [Samples](samples/README.md) — choose a sample by problem; AWS-specific sample READMEs include trimmed incoming events and minimal Terraform topology sketches.
- [Migrating from V5 to V6](MIGRATION.md)
- [Changelog](CHANGELOG.md)

## Packages

| Package | Purpose |
| --- | --- |
| `Kralizek.Lambda.Template` | Core runtime and generic Event/Request/Record programming models |
| `Kralizek.Lambda.Template.Abstractions` | Lightweight handler, context, decoder, and record-result contracts |
| `Kralizek.Lambda.Template.Sqs` | SQS record processing |
| `Kralizek.Lambda.Template.Sns` | SNS notification processing |
| `Kralizek.Lambda.Template.S3` | S3 notifications and S3 Batch Operations |
| `Kralizek.Lambda.Template.EventBridge` | EventBridge events |
| `Kralizek.Lambda.Template.DynamoDbStreams` | DynamoDB Streams |
| `Kralizek.Lambda.Template.KinesisStreams` | Kinesis Streams |
| `Kralizek.Lambda.Template.Cognito` | Cognito triggers |
| `Kralizek.Lambda.Templates` | `dotnet new` project templates |

## Generic event function

```csharp
public sealed class Function : EventFunction<MyEvent, MyEventHandler>;

public sealed class MyEventHandler : IEventHandler<MyEvent>
{
    public ValueTask HandleAsync(
        MyEvent input,
        EventContext context,
        CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
```

## Generic request function

```csharp
public sealed class Function : RequestFunction<MyRequest, MyResponse, MyRequestHandler>;

public sealed class MyRequestHandler : IRequestHandler<MyRequest, MyResponse>
{
    public ValueTask<MyResponse> HandleAsync(
        MyRequest input,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(new MyResponse());
    }
}
```

## Record processing

Record-oriented integrations create an independent dependency-injection scope for each record. Source packages define source-specific result types and translate them into the AWS response expected by that event source.

SQS, for example, supports typed payload decoding and partial batch failure responses:

```csharp
public sealed class Function : SqsFunction<OrderCreated, OrderCreatedHandler>;

public sealed class OrderCreatedHandler : ISqsMessageHandler<OrderCreated>
{
    public ValueTask<SqsRecordResult> HandleAsync(
        OrderCreated message,
        SqsMessageContext context,
        CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(SqsRecordResult.Success);
    }
}
```

DynamoDB Streams and Kinesis Streams deliberately process records sequentially inside one Lambda invocation. Use the event source mapping's `ParallelizationFactor` when additional stream concurrency is required.

## Configuration and services

Function base classes expose hooks for configuration, logging, and services:

```csharp
protected override void ConfigureServices(
    IServiceCollection services,
    IConfiguration configuration)
{
    base.ConfigureServices(services, configuration);
    services.AddSingleton<MyService>();
}
```

Handlers are declared by the function's generic type arguments and are registered automatically. Consumer code does not need a separate handler-registration hook.

## Templates

Install the template package:

```bash
dotnet new install Kralizek.Lambda.Templates
```

List the available templates:

```bash
dotnet new list lambda-template
```

The template set includes generic event/request functions and source-specific templates for supported AWS integrations.

## Build

The repository requires the .NET SDK version pinned in `global.json`.

```bash
dotnet restore
dotnet build --configuration Release --warnaserror
dotnet test --configuration Release
```

See [CONTRIBUTING.md](CONTRIBUTING.md) for contribution guidelines.

## License

MIT. See [LICENSE.txt](LICENSE.txt).
