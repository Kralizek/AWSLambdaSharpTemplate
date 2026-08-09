# Event Functions

Use an event function when a Lambda handles one input and has no application result to return.

```csharp
public sealed class Function : EventFunction<OrderCreated, OrderCreatedHandler>;
```

The handler implements `IEventHandler<TInput>` and receives the deserialized input, an `EventContext`, and a cancellation token derived from the Lambda invocation.

```csharp
public sealed class OrderCreatedHandler : IEventHandler<OrderCreated>
{
    public ValueTask HandleAsync(
        OrderCreated input,
        EventContext context,
        CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
```

One dependency-injection scope is created for the invocation. EventBridge specializes this model while preserving its AWS envelope.
