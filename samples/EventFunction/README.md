# EventFunction sample

Use this sample when a Lambda consumes a typed event but does not need to return an application response.

This sample is intentionally source-neutral. It demonstrates the runtime programming model without assuming whether the Lambda is invoked directly, through a custom integration, or by another AWS service.

```text
JSON input
  → Lambda serializer
  → EventFunction<string, StringEventHandler>
  → StringEventHandler
```

## Example Lambda input

Because the input type in this sample is `string`, a direct test invocation can be as small as:

```json
"hello from Lambda"
```

The Lambda serializer turns that JSON string into the `string` passed to `StringEventHandler`.

`Function` derives from `EventFunction<string, StringEventHandler>`. The framework creates the invocation scope, resolves the handler through dependency injection, and supplies an `EventContext` containing invocation metadata such as the AWS request ID.

There is deliberately no Terraform example here: `EventFunction<TInput, THandler>` is the source-neutral event model. Infrastructure depends on whatever service or application invokes the Lambda.

## Look at

- `Function` for the event-function declaration.
- `StringEventHandler` for constructor injection and access to invocation metadata through `EventContext`.

For the same handler model on the lean host, compare [MinimalEventFunction](../MinimalEventFunction/). For request/response invocations, compare [RequestFunction](../RequestFunction/). For an AWS-specific envelope with a typed payload, see [EventBridgeFunction](../EventBridgeFunction/).
