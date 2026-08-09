# EventBridge

Use `Kralizek.Lambda.Template.EventBridge` for Lambda targets of Amazon EventBridge rules.

```csharp
public sealed record OrderCreated(string OrderId, decimal Total);

public sealed class Function
    : EventBridgeFunction<OrderCreated, OrderCreatedHandler>;
```

Handlers implement `IEventBridgeHandler<TDetail>` and receive `CloudWatchEvent<TDetail>`. The AWS package retains the historical CloudWatch Events type name, while this library uses EventBridge terminology.

`TDetail` is deserialized directly by the Lambda serializer as `CloudWatchEvent<TDetail>.Detail`; there is no additional payload-decoder layer.

The package concerns the receiving pipeline only. Event buses, rules, targets, and publishing belong in application or infrastructure code and the package does not require the EventBridge service client.
