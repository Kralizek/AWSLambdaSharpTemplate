# MinimalEventFunction sample

Use this sample when a Lambda consumes a source-neutral event and you want the v6 handler, context, dependency-injection, logging, and cancellation model without the full invocation processing pipeline.

```text
JSON input
  → Lambda serializer
  → MinimalEventFunction<string, StringEventHandler>
  → StringEventHandler
```

The application handler is the same shape used by the normal `EventFunction` host: `IEventHandler<string>` receives an `EventContext` and `CancellationToken`, and constructor injection works normally.

A direct test invocation can be:

```json
"hello from Lambda"
```

Compare this sample with [EventFunction](../EventFunction/) to see the hosting difference. The handler model stays the same; only the function host changes.
