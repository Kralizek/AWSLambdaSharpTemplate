# OpenTelemetry event function

This sample shows how to instrument a function built with `EventFunction` using the standard `OpenTelemetry.Instrumentation.AWSLambda` package.

The concrete function hides the inherited `FunctionHandlerAsync` method with the same Lambda-compatible signature and delegates to `AWSLambdaWrapper.TraceAsync`, passing the inherited handler as the wrapped delegate. The normal `EventFunction` lifecycle remains unchanged underneath the wrapper.

`AddAWSLambdaConfigurations` configures the OpenTelemetry provider for the Lambda execution environment. This sample disables AWS X-Ray context extraction so tracing also works when X-Ray is not enabled for the function. If your function uses X-Ray propagation, configure the instrumentation accordingly.

The console exporter keeps the sample self-contained and makes the invocation span visible in the Lambda logs. Applications can replace it with an OTLP or other OpenTelemetry exporter without changing the wrapper pattern.

Equivalent samples are also available for `RequestFunction` in `../OpenTelemetryRequestFunction` and for record processing with typed SQS in `../OpenTelemetrySqsFunction`.

Deploy the sample from this directory with:

```shell
dotnet lambda deploy-function
```

Then invoke it with:

```shell
dotnet lambda invoke-function otel-event-function --payload "Hello World"
```
