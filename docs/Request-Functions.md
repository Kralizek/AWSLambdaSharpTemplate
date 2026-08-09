# Request Functions

Use a request function when the Lambda receives an input and must return an application result.

```csharp
public sealed class Function
    : RequestFunction<string, string, ToUpperStringRequestHandler>;
```

```csharp
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

One dependency-injection scope is created for the invocation. Cognito trigger packages specialize this model with fixed AWS input/output contracts.
