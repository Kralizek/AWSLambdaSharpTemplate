# MinimalRequestFunction sample

Use this sample when a Lambda has a source-neutral request/response shape and you want the v6 handler, context, dependency-injection, logging, and cancellation model without the full invocation processing pipeline.

```text
JSON request
  → Lambda serializer
  → MinimalRequestFunction<string, string, UpperCaseHandler>
  → UpperCaseHandler
  → JSON response
```

The application handler is the same shape used by the normal `RequestFunction` host: `IRequestHandler<string, string>` receives a `RequestContext` and `CancellationToken`, and constructor injection works normally.

A direct invocation can use:

```json
"hello from Lambda"
```

and returns:

```json
"HELLO FROM LAMBDA"
```

Compare this sample with [RequestFunction](../RequestFunction/) to see the hosting difference. The handler model stays the same; only the function host changes.
