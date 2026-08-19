using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;

using Kralizek.Lambda;

using Microsoft.Extensions.Logging;

using OpenTelemetry;
using OpenTelemetry.Instrumentation.AWSLambda;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace OpenTelemetryEventFunction;

public class Function : EventFunction<string, StringEventHandler>
{
    private static readonly TracerProvider TracerProvider = Sdk.CreateTracerProviderBuilder()
        .AddAWSLambdaConfigurations(options => options.DisableAwsXRayContextExtraction = true)
        .AddConsoleExporter()
        .AddOtlpExporter()
        .Build();

    protected override void ConfigureLogging(ILoggingBuilder logging) =>
        logging.AddOpenTelemetry(options => options.AddOtlpExporter());

    public new Task FunctionHandlerAsync(string input, ILambdaContext context) =>
        AWSLambdaWrapper.TraceAsync(TracerProvider, base.FunctionHandlerAsync, input, context);
}

public class StringEventHandler(ILogger<StringEventHandler> logger) : IEventHandler<string>
{
    public ValueTask HandleAsync(string input, EventContext context, CancellationToken cancellationToken)
    {
        logger.LogInformation("Input {Input} for request {AwsRequestId}", input, context.AwsRequestId);
        return ValueTask.CompletedTask;
    }
}
