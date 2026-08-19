using Amazon;

var builder = DistributedApplication.CreateBuilder(args);

var aws = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.EUWest1);

builder.AddAWSLambdaFunction<Projects.OpenTelemetryEventFunction>(
        "event-function",
        "OpenTelemetryEventFunction::OpenTelemetryEventFunction.Function::FunctionHandlerAsync")
    .WithReference(aws)
    .WithOtlpExporter();

builder.AddAWSLambdaFunction<Projects.OpenTelemetryRequestFunction>(
        "request-function",
        "OpenTelemetryRequestFunction::OpenTelemetryRequestFunction.Function::FunctionHandlerAsync")
    .WithReference(aws)
    .WithOtlpExporter();

builder.AddAWSLambdaFunction<Projects.OpenTelemetrySqsFunction>(
        "sqs-function",
        "OpenTelemetrySqsFunction::OpenTelemetrySqsFunction.Function::FunctionHandlerAsync")
    .WithReference(aws)
    .WithOtlpExporter();

builder.Build().Run();
