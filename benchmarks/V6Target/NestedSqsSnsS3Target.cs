using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.TestUtilities;

using BenchmarkWorkloads;

using Kralizek.Lambda;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace V6Target;

public sealed class NestedMinimalSqsSnsS3Target : ISqsTarget
{
    private readonly NestedMinimalSqsSnsS3Function _function = new();
    private readonly ILambdaContext _context = new TestLambdaContext
    {
        RemainingTime = TimeSpan.FromMinutes(1)
    };
    private readonly IReadOnlyDictionary<int, SQSEvent> _events = NestedSqsEnvelopeFactory.Create();

    public async Task<int> InvokeAsync(int batchSize)
    {
        var response = await _function.FunctionHandlerAsync(_events[batchSize], _context).ConfigureAwait(false);
        return response.BatchItemFailures?.Count ?? 0;
    }
}

public sealed class NestedSqsSnsS3Target : ISqsTarget
{
    private readonly NestedSqsSnsS3Function _function = new();
    private readonly ILambdaContext _context = new TestLambdaContext
    {
        RemainingTime = TimeSpan.FromMinutes(1)
    };
    private readonly IReadOnlyDictionary<int, SQSEvent> _events = NestedSqsEnvelopeFactory.Create();

    public async Task<int> InvokeAsync(int batchSize)
    {
        var response = await _function.FunctionHandlerAsync(_events[batchSize], _context).ConfigureAwait(false);
        return response.BatchItemFailures?.Count ?? 0;
    }
}

public sealed class NestedAsyncMinimalSqsSnsS3Target : ISqsTarget
{
    private readonly NestedAsyncMinimalSqsSnsS3Function _function = new();
    private readonly ILambdaContext _context = new TestLambdaContext
    {
        RemainingTime = TimeSpan.FromMinutes(1)
    };
    private readonly IReadOnlyDictionary<int, SQSEvent> _events = NestedSqsEnvelopeFactory.Create();

    public async Task<int> InvokeAsync(int batchSize)
    {
        var response = await _function.FunctionHandlerAsync(_events[batchSize], _context).ConfigureAwait(false);
        return response.BatchItemFailures?.Count ?? 0;
    }
}

public sealed class NestedAsyncSqsSnsS3Target : ISqsTarget
{
    private readonly NestedAsyncSqsSnsS3Function _function = new();
    private readonly ILambdaContext _context = new TestLambdaContext
    {
        RemainingTime = TimeSpan.FromMinutes(1)
    };
    private readonly IReadOnlyDictionary<int, SQSEvent> _events = NestedSqsEnvelopeFactory.Create();

    public async Task<int> InvokeAsync(int batchSize)
    {
        var response = await _function.FunctionHandlerAsync(_events[batchSize], _context).ConfigureAwait(false);
        return response.BatchItemFailures?.Count ?? 0;
    }
}

public sealed class NestedMinimalSqsSnsS3Function : MinimalRequestFunction<SQSEvent, SQSBatchResponse, NestedMinimalSqsSnsS3Handler>;

public sealed class NestedMinimalSqsSnsS3Handler : IRequestHandler<SQSEvent, SQSBatchResponse>
{
    public ValueTask<SQSBatchResponse> HandleAsync(
        SQSEvent input,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        foreach (var sqsRecord in input.Records ?? Enumerable.Empty<SQSEvent.SQSMessage>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var snsEnvelope = JsonSerializer.Deserialize<NestedSnsEnvelope>(sqsRecord.Body, NestedMinimalJson.Options)
                ?? throw new JsonException("The benchmark SNS envelope could not be deserialized.");
            var s3Event = JsonSerializer.Deserialize<S3Event>(snsEnvelope.Message, NestedMinimalJson.Options)
                ?? throw new JsonException("The benchmark S3 event payload could not be deserialized.");

#pragma warning disable S3267 // Preserve the explicit Minimal S3 record loop so the benchmark does not add LINQ allocations.
            foreach (var s3Record in s3Event.Records ?? [])
            {
                cancellationToken.ThrowIfCancellationRequested();
                _ = NestedSqsSnsS3Workload.Execute(s3Record.S3.Bucket.Name, s3Record.S3.Object.Key);
            }
#pragma warning restore S3267
        }

        return ValueTask.FromResult(new SQSBatchResponse
        {
            BatchItemFailures = []
        });
    }
}

public sealed class NestedAsyncMinimalSqsSnsS3Function : MinimalRequestFunction<SQSEvent, SQSBatchResponse, NestedAsyncMinimalSqsSnsS3Handler>;

public sealed class NestedAsyncMinimalSqsSnsS3Handler : IRequestHandler<SQSEvent, SQSBatchResponse>
{
    public async ValueTask<SQSBatchResponse> HandleAsync(
        SQSEvent input,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        foreach (var sqsRecord in input.Records ?? Enumerable.Empty<SQSEvent.SQSMessage>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var snsEnvelope = JsonSerializer.Deserialize<NestedSnsEnvelope>(sqsRecord.Body, NestedMinimalJson.Options)
                ?? throw new JsonException("The benchmark SNS envelope could not be deserialized.");
            var s3Event = JsonSerializer.Deserialize<S3Event>(snsEnvelope.Message, NestedMinimalJson.Options)
                ?? throw new JsonException("The benchmark S3 event payload could not be deserialized.");

#pragma warning disable S3267 // Preserve the explicit async Minimal S3 record loop so the benchmark does not add LINQ allocations.
            foreach (var s3Record in s3Event.Records ?? [])
            {
                cancellationToken.ThrowIfCancellationRequested();
                await AsyncWorkload.Suspend();
                cancellationToken.ThrowIfCancellationRequested();
                _ = NestedSqsSnsS3Workload.Execute(s3Record.S3.Bucket.Name, s3Record.S3.Object.Key);
            }
#pragma warning restore S3267
        }

        return new SQSBatchResponse
        {
            BatchItemFailures = []
        };
    }
}

public sealed class NestedSqsSnsS3Function : SqsFunction<NestedSnsEnvelope, NestedSqsSnsS3Handler>
{
    protected override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddS3ObjectEventProcessing<NestedS3ObjectEventHandler>();
        services.TryAddSingleton<IStringPayloadDecoder<S3Event>, JsonStringPayloadDecoder<S3Event>>();
    }
}

public sealed class NestedAsyncSqsSnsS3Function : SqsFunction<NestedSnsEnvelope, NestedSqsSnsS3Handler>
{
    protected override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddS3ObjectEventProcessing<NestedAsyncS3ObjectEventHandler>();
        services.TryAddSingleton<IStringPayloadDecoder<S3Event>, JsonStringPayloadDecoder<S3Event>>();
    }
}

public sealed class NestedSnsEnvelope
{
    public string Message { get; set; } = string.Empty;
}

public sealed class NestedSqsSnsS3Handler : ISqsMessageHandler<NestedSnsEnvelope>
{
    private readonly IStringPayloadDecoder<S3Event> _s3Decoder;
    private readonly IRecordProcessor<
        S3Event.S3EventNotificationRecord,
        S3RecordResult,
        RecordContext> _s3Processor;

    public NestedSqsSnsS3Handler(
        IStringPayloadDecoder<S3Event> s3Decoder,
        IRecordProcessor<S3Event.S3EventNotificationRecord, S3RecordResult, RecordContext> s3Processor)
    {
        _s3Decoder = s3Decoder;
        _s3Processor = s3Processor;
    }

    public async ValueTask<SqsRecordResult> HandleAsync(
        NestedSnsEnvelope message,
        SqsMessageContext context,
        CancellationToken cancellationToken)
    {
        var s3Event = await _s3Decoder.DecodeAsync(message.Message, cancellationToken).ConfigureAwait(false);

        foreach (var record in (IEnumerable<S3Event.S3EventNotificationRecord>?)s3Event.Records ?? Array.Empty<S3Event.S3EventNotificationRecord>())
        {
            await _s3Processor.ProcessAsync(record, context, cancellationToken).ConfigureAwait(false);
        }

        return SqsRecordResult.Success;
    }
}

public sealed class NestedS3ObjectEventHandler : IS3ObjectEventHandler
{
    public ValueTask HandleAsync(
        S3ObjectEvent item,
        S3RecordContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = context.GetSqsMessage();
        _ = NestedSqsSnsS3Workload.Execute(item.Object.Bucket, item.Object.Key);
        return ValueTask.CompletedTask;
    }
}

public sealed class NestedAsyncS3ObjectEventHandler : IS3ObjectEventHandler
{
    public async ValueTask HandleAsync(
        S3ObjectEvent item,
        S3RecordContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = context.GetSqsMessage();

        await AsyncWorkload.Suspend();
        _ = NestedSqsSnsS3Workload.Execute(item.Object.Bucket, item.Object.Key);
    }
}

internal static class NestedMinimalJson
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNameCaseInsensitive = true
    };
}

internal static class NestedSqsEnvelopeFactory
{
    private static readonly int[] BatchSizes = [1, 10];

    public static IReadOnlyDictionary<int, SQSEvent> Create() =>
        BatchSizes.ToDictionary(batchSize => batchSize, CreateEnvelope);

    private static SQSEvent CreateEnvelope(int batchSize) =>
        new()
        {
            Records = Enumerable.Range(0, batchSize)
                .Select(index => new SQSEvent.SQSMessage
                {
                    MessageId = $"message-{index}",
                    Body = NestedSqsSnsS3Workload.SnsEnvelopeJson
                })
                .ToList()
        };
}
