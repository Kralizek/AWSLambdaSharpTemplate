# Deadline cancellation

V6 handlers receive a generic invocation `CancellationToken`. In the managed AWS Lambda runtime there is currently no host-provided cancellation signal, so the full and Minimal hosts pass `CancellationToken.None`.

Handlers that want cooperative cancellation tied to the Lambda timeout can opt in through the invocation context:

```csharp
using var deadline = context.CreateDeadlineCancellationTokenSource();

await DoWorkAsync(deadline.Token);
```

`CreateDeadlineCancellationTokenSource()` creates a new `CancellationTokenSource` from the current `ILambdaContext.RemainingTime`. The caller owns the returned source and must dispose it.

This keeps deadline cancellation completely opt-in: handlers that do not request it pay no cancellation-specific allocation or synchronization cost. The same extension method is available on Request, Event, and Record contexts; Minimal hosting currently covers Request and Event functions.
