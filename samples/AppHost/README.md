# Aspire AppHost sample

This sample shows how existing OpenTelemetry-enabled Lambda function projects in this repository can be composed in a .NET Aspire AppHost without restructuring them into a nested solution.

The AppHost references three existing standalone samples:

- `OpenTelemetryEventFunction`, representing a one-way event function;
- `OpenTelemetryRequestFunction`, representing a request/response function;
- `OpenTelemetrySqsFunction`, representing record-oriented batch processing.

All three functions are registered with the AWS Aspire integration and run locally through the AWS Lambda test tool managed by `Aspire.Hosting.AWS`.

Each function exports OpenTelemetry traces and logs through OTLP. The AppHost enables Aspire OTLP configuration for the Lambda resources, so the Aspire dashboard receives those signals and can display both invocation traces and application logs while the functions run locally.

## Run

Configure the `default` AWS profile, then run:

```bash
dotnet run --project samples/AppHost/AppHost.csproj
```

The AppHost uses `eu-west-1` by default. Change the profile or region in `Program.cs` if needed.

The Lambda projects remain independently runnable samples. The AppHost only demonstrates composing them for local development, orchestration, and local observability through Aspire.
