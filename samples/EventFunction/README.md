# EventFunction sample

Use this sample when a Lambda consumes a typed event but does not need to return an application response.

The function derives from `EventFunction<string, StringEventHandler>`. The handler receives the deserialized input together with `EventContext`, uses constructor injection for logging, and returns `ValueTask.CompletedTask` when processing is complete.

This is the simplest sample for understanding the separation between the Lambda entry point and an injected handler.

## Look at

- `Function` for the minimal function declaration.
- `StringEventHandler` for handler DI and access to invocation metadata through `EventContext`.

For request/response invocations, compare this sample with [RequestFunction](../RequestFunction/).
