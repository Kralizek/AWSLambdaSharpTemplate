using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.S3Events;

using Kralizek.Lambda;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace NativeAotSqsSnsS3Function;

public sealed class Function : SqsFunction<SnsEnvelope, SnsEnvelopedS3DeliveryHandler>
{
    protected override void ConfigureFrameworkServices(IServiceCollection services)
    {
        services.AddSingleton(PayloadJsonSerializerContext.Default.SnsEnvelope);
        services.AddSingleton(PayloadJsonSerializerContext.Default.S3Event);
    }

    protected override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddS3ObjectEventProcessing<S3ObjectEventHandler>();
        services.TryAddScoped<S3EventDispatcher>();
        services.TryAddSingleton<IStringPayloadDecoder<S3Event>>(
            new JsonStringPayloadDecoder<S3Event>(PayloadJsonSerializerContext.Default.S3Event));
    }
}

public sealed record SnsEnvelope
{
    public string Message { get; init; } = string.Empty;
}

public sealed class SnsEnvelopedS3DeliveryHandler : ISqsMessageHandler<SnsEnvelope>
{
    private readonly IStringPayloadDecoder<S3Event> _decoder;
    private readonly S3EventDispatcher _dispatcher;

    public SnsEnvelopedS3DeliveryHandler(
        IStringPayloadDecoder<S3Event> decoder,
        S3EventDispatcher dispatcher)
    {
        _decoder = decoder;
        _dispatcher = dispatcher;
    }

    public async ValueTask<SqsRecordResult> HandleAsync(
        SnsEnvelope message,
        SqsMessageContext context,
        CancellationToken cancellationToken)
    {
        var s3Event = await _decoder.DecodeAsync(message.Message, cancellationToken).ConfigureAwait(false);
        await _dispatcher.DispatchAsync(s3Event, context, cancellationToken).ConfigureAwait(false);
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
            "Handling S3 object {Bucket}/{Key} from SNS via SQS message {MessageId}",
            item.Object.Bucket,
            item.Object.Key,
            sqsMessage.MessageId);

        return ValueTask.CompletedTask;
    }
}

[JsonSerializable(typeof(SnsEnvelope))]
[JsonSerializable(typeof(S3Event))]
internal partial class PayloadJsonSerializerContext : JsonSerializerContext;
