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

namespace RawSdkTarget;

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

public sealed class NestedSqsSnsS3Function
{
    public Task<SQSBatchResponse> FunctionHandlerAsync(SQSEvent input, ILambdaContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var sqsRecord in input.Records ?? Enumerable.Empty<SQSEvent.SQSMessage>())
        {
            var snsEnvelope = JsonSerializer.Deserialize<NestedSnsEnvelope>(sqsRecord.Body, NestedJson.Options)
                ?? throw new JsonException("The benchmark SNS envelope could not be deserialized.");
            var s3Event = JsonSerializer.Deserialize<S3Event>(snsEnvelope.Message, NestedJson.Options)
                ?? throw new JsonException("The benchmark S3 event payload could not be deserialized.");

#pragma warning disable S3267 // Preserve the explicit raw S3 record loop so the benchmark does not add LINQ allocations.
            foreach (var s3Record in s3Event.Records ?? [])
            {
                _ = NestedSqsSnsS3Workload.Execute(s3Record.S3.Bucket.Name, s3Record.S3.Object.Key);
            }
#pragma warning restore S3267
        }

        return Task.FromResult(new SQSBatchResponse
        {
            BatchItemFailures = []
        });
    }
}

public sealed class NestedAsyncSqsSnsS3Function
{
    public async Task<SQSBatchResponse> FunctionHandlerAsync(SQSEvent input, ILambdaContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var sqsRecord in input.Records ?? Enumerable.Empty<SQSEvent.SQSMessage>())
        {
            var snsEnvelope = JsonSerializer.Deserialize<NestedSnsEnvelope>(sqsRecord.Body, NestedJson.Options)
                ?? throw new JsonException("The benchmark SNS envelope could not be deserialized.");
            var s3Event = JsonSerializer.Deserialize<S3Event>(snsEnvelope.Message, NestedJson.Options)
                ?? throw new JsonException("The benchmark S3 event payload could not be deserialized.");

#pragma warning disable S3267 // Preserve the explicit async raw S3 record loop so the benchmark does not add LINQ allocations.
            foreach (var s3Record in s3Event.Records ?? [])
            {
                await AsyncWorkload.Suspend();
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
