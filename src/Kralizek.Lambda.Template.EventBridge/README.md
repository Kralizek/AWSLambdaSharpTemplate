# Kralizek.Lambda.Template.EventBridge

EventBridge specialization for `Kralizek.Lambda.Template`.

The package builds directly on the source-neutral `EventFunction` programming model and uses AWS's `Amazon.Lambda.CloudWatchEvents` event model. The AWS package keeps its historical CloudWatch Events name, while this package uses the current EventBridge terminology.

## Typed EventBridge function

Define the event detail contract and a scoped handler:

```csharp
public sealed record OrderCreated(string OrderId, decimal Total);

public sealed class OrderCreatedHandler(ILogger<OrderCreatedHandler> logger)
    : IEventBridgeHandler<OrderCreated>
{
    public ValueTask HandleAsync(
        CloudWatchEvent<OrderCreated> input,
        EventContext context,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing order {OrderId} from {Source} ({DetailType})",
            input.Detail.OrderId,
            input.Source,
            input.DetailType);

        return ValueTask.CompletedTask;
    }
}
```

Then expose the Lambda entry point:

```csharp
public sealed class Function : EventBridgeFunction<OrderCreated, OrderCreatedHandler>;
```

`TDetail` is deserialized directly by the Lambda serializer as `CloudWatchEvent<TDetail>.Detail`; there is no additional payload-decoder layer. The complete AWS EventBridge envelope remains available to the handler through `CloudWatchEvent<TDetail>`.

The package does not depend on the EventBridge service client. Event buses, rules, targets, and publishing are infrastructure/application concerns rather than part of the Lambda receiving pipeline.
