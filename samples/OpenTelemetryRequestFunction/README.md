# OpenTelemetry request-function experiment

This sample explores whether the standard OpenTelemetry AWS Lambda instrumentation can wrap a function built with `RequestFunction` without changing the framework.

The function accepts a string and returns its upper-case representation. The inherited `FunctionHandlerAsync` method is hidden with a method that has the same Lambda handler signature. That method delegates to `AWSLambdaWrapper.TraceAsync`, passing the inherited handler as the wrapped delegate.

The sample deliberately uses the console exporter so the experiment has no external collector dependency. It is not intended as production OpenTelemetry configuration.

The experiment is successful if the project builds, Lambda resolves the derived `FunctionHandlerAsync` as the configured handler, the request still flows through the existing framework lifecycle, and the standard AWS Lambda instrumentation emits the invocation span.
