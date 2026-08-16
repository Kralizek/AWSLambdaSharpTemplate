using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.Lambda.SQSEvents;

using Kralizek.Lambda;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace SqsSnsS3Function;

public sealed class Function : SqsFunction<SqsSnsS3Handler>
{
    protected override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddS3ObjectEventProcessing<S3ObjectEventHandler>();
        services.TryAddSingleton<IStringPayloadDecoder<SnsEnvelope>, JsonStringPayloadDecoder<SnsEnvelope>>();
        services.TryAddSingleton<IStringPayloadDecoder<S3Event>, JsonStringPayloadDecoder<S3Event>>();
    }
}

public sealed record SnsEnvelope
{
    public string Message { get; init; } = string.Empty;
}

public sealed class SqsSnsS3Handler : ISqsRecordHandler
{
    private readonly IStringPayloadDecoder<SnsEnvelope> _snsDecoder;
    private readonly IStringPayloadDecoder<S3Event> _s3Decoder;
    private readonly IRecordProcessor<
        S3Event.S3EventNotificationRecord,
        S3RecordResult,
        RecordContext> _s3Processor;

    public SqsSnsS3Handler(
        IStringPayloadDecoder<SnsEnvelope> snsDecoder,
        IStringPayloadDecoder<S3Event> s3Decoder,
        IRecordProcessor<S3Event.S3EventNotificationRecord, S3RecordResult, RecordContext> s3Processor)
    {
        _snsDecoder = snsDecoder;
        _s3Decoder = s3Decoder;
        _s3Processor = s3Processor;
    }

    public async ValueTask<SqsRecordResult> HandleAsync(
        SQSEvent.SQSMessage record,
        SqsMessageContext context,
        CancellationToken cancellationToken)
    {
        var snsEnvelope = await _snsDecoder.DecodeAsync(record.Body, cancellationToken).ConfigureAwait(false);
        var s3Event = await _s3Decoder.DecodeAsync(snsEnvelope.Message, cancellationToken).ConfigureAwait(false);

        foreach (var s3Record in s3Event.Records ?? new List<S3Event.S3EventNotificationRecord>())
        {
            await _s3Processor.ProcessAsync(s3Record, context, cancellationToken).ConfigureAwait(false);
        }

        return SqsRecordResult.Success;
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
