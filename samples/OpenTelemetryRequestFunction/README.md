# OpenTelemetry request function

This sample shows how to instrument a function built with `RequestFunction` using the standard `OpenTelemetry.Instrumentation.AWSLambda` package.

The function accepts a string and returns its upper-case representation. The concrete function hides the inherited `FunctionHandlerAsync` method with a method that has the same Lambda-compatible signature and delegates to `AWSLambdaWrapper.TraceAsync`, passing the inherited handler as the wrapped delegate.

`AddAWSLambdaConfigurations` configures the OpenTelemetry provider for the Lambda execution environment. This sample disables AWS X-Ray context extraction so tracing also works when X-Ray is not enabled for the function. If your function uses X-Ray propagation, configure the instrumentation accordingly.

The sample exports traces to both the console and OTLP. It also adds an OpenTelemetry logging provider that exports `ILogger` records through OTLP. The OTLP exporter follows the standard OpenTelemetry environment variables, so the destination can be supplied by the hosting environment without changing the function code.

When this sample is run through `../AppHost`, Aspire provides the OTLP endpoint and related OpenTelemetry environment variables. Invocation traces and application logs are then available in the Aspire dashboard.

Equivalent samples are also available for `EventFunction` in `../OpenTelemetryEventFunction` and for record processing with typed SQS in `../OpenTelemetrySqsFunction`.

Deploy the sample from this directory with:

```shell
dotnet lambda deploy-function
```

Then invoke it with:

```shell
dotnet lambda invoke-function otel-request-function --payload "Hello World"
```

The invocation returns `"HELLO WORLD"` and the Lambda logs include the server span emitted by the standard AWS Lambda OpenTelemetry instrumentation.

The wrapper pattern in this sample has also been validated against the AWS Lambda .NET 10 runtime: Lambda resolves the derived handler, the request continues through the normal `RequestFunction` lifecycle, and the standard AWS Lambda instrumentation exports the invocation span.
