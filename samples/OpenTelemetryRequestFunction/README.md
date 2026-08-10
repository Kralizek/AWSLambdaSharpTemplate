# OpenTelemetry request function

This sample shows how to instrument a function built with `RequestFunction` using the standard `OpenTelemetry.Instrumentation.AWSLambda` package.

The function accepts a string and returns its upper-case representation. The concrete function hides the inherited `FunctionHandlerAsync` method with a method that has the same Lambda-compatible signature and delegates to `AWSLambdaWrapper.TraceAsync`, passing the inherited handler as the wrapped delegate.

`AddAWSLambdaConfigurations` configures the OpenTelemetry provider for the Lambda execution environment. This sample disables AWS X-Ray context extraction so tracing also works when X-Ray is not enabled for the function. If your function uses X-Ray propagation, configure the instrumentation accordingly.

The console exporter keeps the sample self-contained and makes the invocation span visible in the Lambda logs. Applications can replace it with an OTLP or other OpenTelemetry exporter without changing the wrapper pattern.

Deploy the sample from this directory with:

```shell
dotnet lambda deploy-function
```

Then invoke it with:

```shell
dotnet lambda invoke-function otel-request-function --payload "Hello World"
```

The invocation returns `"HELLO WORLD"` and the Lambda logs include the server span emitted by the standard AWS Lambda OpenTelemetry instrumentation.
