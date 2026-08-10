# RequestFunction sample

Use this sample when a Lambda has a typed request and returns a typed response.

The function derives from `RequestFunction<string, string, UpperCaseHandler>`. `UpperCaseHandler` receives the request together with `RequestContext`, logs the AWS request ID, and returns an upper-case version of the input.

The example is intentionally trivial so the request/response programming model is the only moving part.

## Look at

- `Function` for the input, output, and handler types declared in one place.
- `UpperCaseHandler` for constructor injection, `RequestContext`, and returning a response asynchronously.

If the invocation only needs to consume an event, compare this sample with [EventFunction](../EventFunction/).
