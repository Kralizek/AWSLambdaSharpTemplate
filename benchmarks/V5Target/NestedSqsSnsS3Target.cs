#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.TestUtilities;

using BenchmarkWorkloads;

using Kralizek.Lambda;

using Microsoft.Extensions.DependencyInjection;

namespace V5Target;

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
        await _function.FunctionHandlerAsync(_events[batchSize], _context).ConfigureAwait(false);
        return 0;
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
        await _function.FunctionHandlerAsync(_events[batchSize], _context).ConfigureAwait(false);
        return 0;
    }
}

public sealed class NestedSqsSnsS3Function : EventFunction<SQSEvent>
{
    protected override void ConfigureServices(IServiceCollection services, IExecutionEnvironment executionEnvironment) =>
        services.UseQueueMessageHandler<NestedSnsEnvelope, NestedSqsSnsS3Handler>();
}

public sealed class NestedSqsSnsS3Handler : IMessageHandler<NestedSnsEnvelope>
{
    public Task HandleAsync(NestedSnsEnvelope? message, ILambdaContext context)
    {
        ArgumentNullException.ThrowIfNull(message);

        var s3Event = JsonSerializer.Deserialize<S3Event>(message.Message, NestedJson.Options)
            ?? throw new JsonException("The benchmark S3 event payload could not be deserialized.");

#pragma warning disable S3267 // Preserve the explicit V5 S3 record loop so the benchmark measures consumer-owned iteration without LINQ allocations.
        foreach (var s3Record in s3Event.Records ?? [])
        {
            _ = NestedSqsSnsS3Workload.Execute(s3Record.S3.Bucket.Name, s3Record.S3.Object.Key);
        }
#pragma warning restore S3267

        return Task.CompletedTask;
    }
}

public sealed class NestedAsyncSqsSnsS3Function : EventFunction<SQSEvent>
{
    protected override void ConfigureServices(IServiceCollection services, IExecutionEnvironment executionEnvironment) =>
        services.UseQueueMessageHandler<NestedSnsEnvelope, NestedAsyncSqsSnsS3Handler>();
}

public sealed class NestedAsyncSqsSnsS3Handler : IMessageHandler<NestedSnsEnvelope>
{
    public async Task HandleAsync(NestedSnsEnvelope? message, ILambdaContext context)
    {
        ArgumentNullException.ThrowIfNull(message);

        var s3Event = JsonSerializer.Deserialize<S3Event>(message.Message, NestedJson.Options)
            ?? throw new JsonException("The benchmark S3 event payload could not be deserialized.");

#pragma warning disable S3267 // Preserve the explicit async V5 S3 record loop so the benchmark does not add LINQ allocations.
        foreach (var s3Record in s3Event.Records ?? [])
        {
            await AsyncWorkload.Suspend();
            _ = NestedSqsSnsS3Workload.Execute(s3Record.S3.Bucket.Name, s3Record.S3.Object.Key);
        }
#pragma warning restore S3267
    }
}

public sealed class NestedSnsEnvelope
{
    public string Message { get; set; } = string.Empty;
}

internal static class NestedJson
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
