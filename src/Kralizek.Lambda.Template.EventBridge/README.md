# Kralizek.Lambda.Template.EventBridge

Amazon EventBridge specialization for strongly typed event details while preserving the complete AWS event envelope.

```csharp
public sealed class Function : EventBridgeFunction<OrderCreated, OrderCreatedHandler>;
```

For usage, processing semantics, and examples, see [EventBridge](https://github.com/Kralizek/AWSLambdaSharpTemplate/blob/HEAD/docs/EventBridge.md).

The complete library documentation is available in the [`docs/` directory](https://github.com/Kralizek/AWSLambdaSharpTemplate/blob/HEAD/docs/README.md).
