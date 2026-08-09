# Customization

Function bases expose focused hooks for application customization while keeping runtime wiring inside the framework.

## Services

Register application dependencies with `ConfigureServices`:

```csharp
protected override void ConfigureServices(
    IServiceCollection services,
    IConfiguration configuration)
{
    services.AddScoped<IMyService, MyService>();
    base.ConfigureServices(services, configuration);
}
```

The primary handler is registered automatically.

## Configuration and logging

Override `ConfigureConfiguration` and `ConfigureLogging` when needed. Lambda-compatible logging is configured by default, so `ILogger<T>` works without additional bootstrap code.

## Decoders

For integrations that use payload decoders, register another `IStringPayloadDecoder<T>` or `IBinaryPayloadDecoder<T>` to replace the default implementation. This is also the seam for source-generated System.Text.Json metadata or non-JSON serialization formats.
