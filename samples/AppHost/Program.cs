using Amazon;

var builder = DistributedApplication.CreateBuilder(args);

var aws = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.EUWest1);

builder.AddAWSLambdaFunction<Projects.RequestFunction>(
        "request-function",
        "RequestFunction::RequestFunction.Function::FunctionHandlerAsync")
    .WithReference(aws);

builder.AddAWSLambdaFunction<Projects.SqsFunction>(
        "sqs-function",
        "SqsFunction::SqsFunction.Function::FunctionHandlerAsync")
    .WithReference(aws);

builder.Build().Run();
