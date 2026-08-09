# Getting Started

The quickest way to start is with the template package.

```bash
dotnet new install Kralizek.Lambda.Templates
```

Choose a template based on the Lambda invocation semantics, not just the AWS service name. Generic event and request templates are available alongside source-specific templates for supported AWS integrations.

For example, create a one-way event function with:

```bash
dotnet new lambda-template-event -n MyLambda
```

A generated project keeps the Lambda entry point intentionally small. Application logic lives in a handler that participates in dependency injection.

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

Use `ConfigureServices` on the function only for application dependencies. The handler itself is registered automatically.

Next: [Choosing a Function Model](Choosing-a-Function-Model.md).
