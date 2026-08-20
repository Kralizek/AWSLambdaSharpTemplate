# Lazy deadline cancellation

V6 request handlers receive a generic invocation `CancellationToken`. In the managed AWS Lambda runtime there is currently no host-provided cancellation signal, so the request hosts pass `CancellationToken.None`.

Handlers that want cooperative cancellation tied to the Lambda timeout can opt in through the invocation context:

```csharp
var deadlineCancellationToken = context.GetDeadlineCancellationToken();
```

The deadline token is created lazily on first access from the current `ILambdaContext.RemainingTime`, cached for the lifetime of the invocation context, and disposed by the host after the handler completes. Calling the accessor repeatedly during the same invocation returns the same token and does not create another timer-backed cancellation source.

This separates generic host cancellation from AWS-specific deadline cancellation and avoids allocating a deadline timer for handlers that do not use it.

This document currently describes the Request host behavior implemented by the accompanying change. Event and Record hosts will be aligned after the Request-host behavior and API shape are validated.
