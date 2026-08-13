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

All project templates also accept `--otel` to generate the function with OpenTelemetry enabled:

```bash
dotnet new lambda-template-request --otel
dotnet new lambda-template-sqs --otel
```

OpenTelemetry is a template-time opt-in. Without `--otel`, the generated project contains no OpenTelemetry code, package references, build properties, or helper files. With `--otel`, the template adds the standard AWS Lambda OpenTelemetry instrumentation, subscribes to the framework activity source and meter, and exports through OTLP. See [OpenTelemetry](OpenTelemetry.md) for the telemetry model, X-Ray context behavior, and exporter customization.

The SQS, SNS, and Kinesis Streams templates generate decoded payload handlers by default. Pass `--raw` when application code should receive the original AWS record instead:

```bash
dotnet new lambda-template-sqs --raw
dotnet new lambda-template-sns --raw
dotnet new lambda-template-kinesis-stream --raw
```

Raw mode changes only the generated handler shape. It uses the source-specific raw record handler contract and omits the sample decoded payload and handler files, while the surrounding Lambda function model and optional `--otel` instrumentation remain unchanged.

The Cognito pre-token-generation template also accepts `--version v1|v2`.

Treat profile, region, role, exporter, and other operational defaults as starting values and adjust them for the target environment.
