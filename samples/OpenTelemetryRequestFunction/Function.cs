using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;

using Kralizek.Lambda;

using Microsoft.Extensions.Logging;

using OpenTelemetry;
using OpenTelemetry.Instrumentation.AWSLambda;
using OpenTelemetry.Trace;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace OpenTelemetryRequestFunction;

public class Function : RequestFunction<string, string, UpperCaseHandler>
{
    private static readonly TracerProvider TracerProvider = Sdk.CreateTracerProviderBuilder()
        .AddAWSLambdaConfigurations()
        .AddConsoleExporter()
        .Build();

    public new Task<string> FunctionHandlerAsync(string input, ILambdaContext context) =>
        AWSLambdaWrapper.TraceAsync(TracerProvider, base.FunctionHandlerAsync, input, context);
}

public class UpperCaseHandler : IRequestHandler<string, string>
{
    private readonly ILogger<UpperCaseHandler> _logger;

    public UpperCaseHandler(ILogger<UpperCaseHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask<string> HandleAsync(string input, RequestContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Input {Input} for request {AwsRequestId}", input, context.AwsRequestId);
        return new ValueTask<string>(input.ToUpperInvariant());
    }
}
