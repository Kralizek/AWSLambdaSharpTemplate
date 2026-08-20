# Project templates

The template pack is published as `Kralizek.Lambda.Templates` and contains project templates for the generic request/event models and the supported AWS event sources.

## Build the template pack

```bash
dotnet pack templates/Kralizek.Lambda.Templates.csproj -c Release
```

## Install and try the templates

```bash
dotnet new install Kralizek.Lambda.Templates
dotnet new list lambda-template
```

## Template parameters

All project templates accept `--profile`, `--region`, and `--role` to populate the generated `aws-lambda-tools-defaults.json` file.

The generic Request and Event templates accept `--minimal` to use the lean host while keeping the same handler contract and application files:

```bash
dotnet new lambda-template-request --minimal
dotnet new lambda-template-event --minimal
```

Minimal hosting retains configuration, logging, dependency injection, one invocation scope, the standard function context, cancellation, and asynchronous scope disposal. It deliberately omits the normal host's internal telemetry and richer processing pipeline. See [Minimal Hosting](Minimal-Hosting.md) for the capability boundary and guidance on choosing a host.

All project templates also accept `--otel` to generate the function with OpenTelemetry enabled:

```bash
dotnet new lambda-template-request --otel
dotnet new lambda-template-sqs --otel
```

OpenTelemetry is a template-time opt-in. Without `--otel`, the generated project contains no OpenTelemetry code, package references, build properties, or helper files. With `--otel`, the template adds the standard AWS Lambda OpenTelemetry instrumentation and exports through OTLP.

For the normal host, generated OpenTelemetry code also subscribes to the framework activity source and meter. With `--minimal --otel`, the AWS Lambda wrapper still traces the invocation, but the generated function does not subscribe to KLT's internal `ActivitySource` or `Meter` because Minimal does not emit those signals. Application code remains free to configure its own instrumentation. See [OpenTelemetry](OpenTelemetry.md) for the telemetry model, X-Ray context behavior, and exporter customization.

All project templates accept `--aot` to generate a Native AOT executable using `LambdaBootstrapBuilder` and source-generated System.Text.Json metadata:

```bash
dotnet new lambda-template-request --aot
dotnet new lambda-template-sqs --aot
```

AOT changes executable hosting and serialization, not the framework handler programming model. A normal generated project is a managed Lambda library with an `Assembly::Type::FunctionHandlerAsync` handler and a `LambdaSerializer` assembly attribute. An AOT project enables `PublishAot`, adds `Program.cs` with `LambdaBootstrapBuilder` and `SourceGeneratorLambdaJsonSerializer`, uses the executable assembly as the Lambda handler, and has no `LambdaSerializer` assembly attribute. Both target the managed .NET 10 Lambda runtime. See [Native AOT](Native-AOT.md) for serializer metadata ownership, nested payload decoding, and deployment details.

The SQS, SNS, and Kinesis Streams templates generate decoded payload handlers by default. Pass `--raw` when application code should receive the original AWS record instead:

```bash
dotnet new lambda-template-sqs --raw
dotnet new lambda-template-sns --raw
dotnet new lambda-template-kinesis-stream --raw
```

Raw mode changes only the generated handler shape. It uses the source-specific raw record handler contract and omits typed nested-payload metadata because no application payload is decoded.

The opt-in dimensions describe independent concerns where the selected template supports them:

```text
--minimal = lean request/event hosting
--raw     = record/payload shape
--aot     = executable hosting + serialization
--otel    = invocation instrumentation
```

They can be composed without selecting a separate template family when those options apply to the same template:

```bash
dotnet new lambda-template-event --minimal --otel
dotnet new lambda-template-request --minimal --aot
dotnet new lambda-template-sns --aot --raw
dotnet new lambda-template-kinesis-stream --aot --otel --raw
```

V6.0 exposes `--minimal` only on the source-neutral Request and Event templates. Source-specific Minimal hosts, including Minimal record functions, are intentionally outside the initial scope; consequently no current generated source-specific template combines `--minimal` and `--raw`.

The Cognito pre-token-generation template also accepts `--version v1|v2`.

Treat profile, region, role, exporter, and other operational defaults as starting values and adjust them for the target environment.
