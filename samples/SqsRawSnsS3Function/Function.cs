using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;

using Kralizek.Lambda;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace SqsRawSnsS3Function;

public sealed class Function : SqsFunction<S3Event, S3DeliveryHandler>
{
    protected override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddS3ObjectEventProcessing<S3ObjectEventHandler>();
        services.TryAddScoped<S3EventDispatcher>();
    }
}

public sealed class S3DeliveryHandler : ISqsMessageHandler<S3Event>
{
    private readonly S3EventDispatcher _dispatcher;

    public S3DeliveryHandler(S3EventDispatcher dispatcher) => _dispatcher = dispatcher;

    public async ValueTask<SqsRecordResult> HandleAsync(
        S3Event message,
        SqsMessageContext context,
        CancellationToken cancellationToken)
    {
        await _dispatcher.DispatchAsync(message, context, cancellationToken).ConfigureAwait(false);
        return SqsRecordResult.Success;
    }
}

public sealed class S3EventDispatcher
{
    private readonly IRecordProcessor<
        S3Event.S3EventNotificationRecord,
        S3RecordResult,
        RecordContext> _processor;

    public S3EventDispatcher(
        IRecordProcessor<S3Event.S3EventNotificationRecord, S3RecordResult, RecordContext> processor) =>
        _processor = processor;

    public async ValueTask DispatchAsync(
        S3Event s3Event,
        RecordContext context,
        CancellationToken cancellationToken)
    {
        foreach (var record in s3Event.Records ?? new List<S3Event.S3EventNotificationRecord>())
        {
            await _processor.ProcessAsync(record, context, cancellationToken).ConfigureAwait(false);
        }
    }
}

public sealed class S3ObjectEventHandler : IS3ObjectEventHandler
{
    private readonly ILogger<S3ObjectEventHandler> _logger;

    public S3ObjectEventHandler(ILogger<S3ObjectEventHandler> logger) => _logger = logger;

    public ValueTask HandleAsync(
        S3ObjectEvent item,
        S3RecordContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sqsMessage = context.GetSqsMessage();

        _logger.LogInformation(
            "Handling S3 object {Bucket}/{Key} from SQS message {MessageId}",
            item.Object.Bucket,
            item.Object.Key,
            sqsMessage.MessageId);

        return ValueTask.CompletedTask;
    }
}