# RequestFunction sample

Use this sample when a Lambda has a typed request and returns a typed response.

This sample is intentionally source-neutral. It demonstrates the request/response programming model without tying it to API Gateway, Cognito, direct invocation, or another AWS integration.

```text
JSON request
  → Lambda serializer
  → RequestFunction<string, string, UpperCaseHandler>
  → UpperCaseHandler
  → JSON response
```

## Example Lambda input and output

Because the input and output types are both `string`, a direct invocation can use:

```json
"hello from Lambda"
```

The handler returns:

```json
"HELLO FROM LAMBDA"
```

`Function` derives from `RequestFunction<string, string, UpperCaseHandler>`. The framework creates the invocation scope, resolves `UpperCaseHandler` through dependency injection, passes a `RequestContext`, and serializes the returned string as the Lambda response.

There is deliberately no Terraform example here: `RequestFunction<TInput, TOutput, THandler>` models request/response semantics independently of the service invoking the Lambda. AWS-specific request/response integrations can specialize this model when they need additional envelope behavior.

## Look at

- `Function` for the input, output, and handler types declared in one place.
- `UpperCaseHandler` for constructor injection, `RequestContext`, and returning a response asynchronously.

For the same handler model on the lean host, compare [MinimalRequestFunction](../MinimalRequestFunction/). If the invocation only needs to consume an event, compare this sample with [EventFunction](../EventFunction/). For a concrete AWS request/response trigger, see [CognitoPreSignUpFunction](../CognitoPreSignUpFunction/).
