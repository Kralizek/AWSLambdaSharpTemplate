namespace EventBridgeFunction;

public sealed record OrderCreated(string OrderId, decimal Total);