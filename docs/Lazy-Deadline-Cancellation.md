# Lazy deadline cancellation

V6 handlers receive a generic invocation `CancellationToken`. In the managed AWS Lambda runtime there is currently no host-provided cancellation signal, so the full and Minimal hosts pass `CancellationToken.None`.

Handlers that want cooperative cancellation tied to the Lambda timeout can opt in through the invocation context:

```csharp
var deadlineCancellationToken = context.GetDeadlineCancellationToken();
```

The deadline token is created lazily on first access from the current `ILambdaContext.RemainingTime`, cached for the lifetime of the invocation context, and disposed by the host after the handler completes. Calling the accessor repeatedly during the same invocation returns the same token and does not create another cancellation source.

This separates generic host cancellation from AWS-specific deadline cancellation and avoids allocating deadline-cancellation infrastructure for handlers that do not use it. The same semantics apply to Request, Event, and Record contexts; Minimal hosting currently covers Request and Event functions.
