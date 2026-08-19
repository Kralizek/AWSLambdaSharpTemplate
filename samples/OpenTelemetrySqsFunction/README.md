# OpenTelemetry SQS function

This sample shows how to instrument a typed SQS function using the standard `OpenTelemetry.Instrumentation.AWSLambda` package.

The concrete function hides the inherited `FunctionHandlerAsync` method with the same Lambda-compatible `SQSEvent`/`SQSBatchResponse` signature and delegates to `AWSLambdaWrapper.TraceAsync`, passing the inherited handler as the wrapped delegate. The normal SQS record-processing and partial-batch-response behavior remains unchanged underneath the wrapper.

`AddAWSLambdaConfigurations` configures the OpenTelemetry provider for the Lambda execution environment. This sample disables AWS X-Ray context extraction so tracing also works when X-Ray is not enabled for the function. If your function uses X-Ray propagation, configure the instrumentation accordingly.

The sample exports traces to both the console and OTLP. It also adds an OpenTelemetry logging provider that exports `ILogger` records through OTLP. The OTLP exporter follows the standard OpenTelemetry environment variables, so the destination can be supplied by the hosting environment without changing the function code.

When this sample is run through `../AppHost`, Aspire provides the OTLP endpoint and related OpenTelemetry environment variables. Invocation traces and application logs are then available in the Aspire dashboard.

Equivalent samples are also available for `RequestFunction` in `../OpenTelemetryRequestFunction` and for `EventFunction` in `../OpenTelemetryEventFunction`.

Deploy the sample from this directory with:

```shell
dotnet lambda deploy-function
```

To exercise the function in AWS, configure an SQS event source mapping for the deployed function and send messages whose body matches the sample payload:

```json
{"OrderId":"order-123"}
```
