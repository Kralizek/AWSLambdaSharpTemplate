# Aspire AppHost sample

This sample shows how existing Lambda function projects in this repository can be composed in a .NET Aspire AppHost without restructuring them into a nested solution.

The AppHost references three existing standalone samples:

- `EventFunction`, representing a one-way event function;
- `RequestFunction`, representing a request/response function;
- `SqsFunction`, representing record-oriented batch processing.

All three functions are registered with the AWS Aspire integration and run locally through the AWS Lambda test tool managed by `Aspire.Hosting.AWS`.

## Run

Configure the `default` AWS profile, then run:

```bash
dotnet run --project samples/AppHost/AppHost.csproj
```

The AppHost uses `eu-west-1` by default. Change the profile or region in `Program.cs` if needed.

The Lambda projects remain independently runnable samples. The AppHost only demonstrates composing them for local development and orchestration.
