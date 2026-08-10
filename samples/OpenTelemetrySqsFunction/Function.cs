using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;

using Kralizek.Lambda;

using Microsoft.Extensions.Logging;

using OpenTelemetry;
using OpenTelemetry.Instrumentation.AWSLambda;
using OpenTelemetry.Trace;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace OpenTelemetrySqsFunction;

public class Function : SqsFunction<OrderCreated, OrderCreatedHandler>
{
    private static readonly TracerProvider TracerProvider = Sdk.CreateTracerProviderBuilder()
        .AddAWSLambdaConfigurations(options => options.DisableAwsXRayContextExtraction = true)
        .AddConsoleExporter()
        .Build();

    public new Task<SQSBatchResponse> FunctionHandlerAsync(SQSEvent input, ILambdaContext context) =>
        AWSLambdaWrapper.TraceAsync(TracerProvider, base.FunctionHandlerAsync, input, context);
}

public sealed record OrderCreated(string OrderId);

public sealed class OrderCreatedHandler(ILogger<OrderCreatedHandler> logger)
    : ISqsMessageHandler<OrderCreated>
{
    public ValueTask<SqsRecordResult> HandleAsync(
        OrderCreated message,
        SqsMessageContext context,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing order {OrderId} from SQS message {MessageId}",
            message.OrderId,
            context.MessageId);

        return ValueTask.FromResult(SqsRecordResult.Success);
    }
}
