using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.TestUtilities;

using BenchmarkWorkloads;

namespace RawSdkTarget;

public sealed class UppercaseSqsTarget : ISqsTarget
{
    private readonly UppercaseSqsFunction _function = new();
    private readonly ILambdaContext _context = new TestLambdaContext
    {
        RemainingTime = TimeSpan.FromMinutes(1)
    };
    private readonly IReadOnlyDictionary<int, SQSEvent> _events = SqsEnvelopeFactory.Create();

    public async Task<int> InvokeAsync(int batchSize)
    {
        var response = await _function.FunctionHandlerAsync(_events[batchSize], _context).ConfigureAwait(false);
        return response.BatchItemFailures?.Count ?? 0;
    }
}

public sealed class UppercaseAsyncSqsTarget : ISqsTarget
{
    private readonly UppercaseAsyncSqsFunction _function = new();
    private readonly ILambdaContext _context = new TestLambdaContext
    {
        RemainingTime = TimeSpan.FromMinutes(1)
    };
    private readonly IReadOnlyDictionary<int, SQSEvent> _events = SqsEnvelopeFactory.Create();

    public async Task<int> InvokeAsync(int batchSize)
    {
        var response = await _function.FunctionHandlerAsync(_events[batchSize], _context).ConfigureAwait(false);
        return response.BatchItemFailures?.Count ?? 0;
    }
}

public sealed class UppercaseSqsFunction
{
    public Task<SQSBatchResponse> FunctionHandlerAsync(SQSEvent input, ILambdaContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var record in input.Records ?? Enumerable.Empty<SQSEvent.SQSMessage>())
        {
            var message = JsonSerializer.Deserialize<SqsBenchmarkMessage>(record.Body)!;
            _ = SqsWorkload.Execute(message);
        }

        return Task.FromResult(new SQSBatchResponse
        {
            BatchItemFailures = []
        });
    }
}

public sealed class UppercaseAsyncSqsFunction
{
    public async Task<SQSBatchResponse> FunctionHandlerAsync(SQSEvent input, ILambdaContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var record in input.Records ?? Enumerable.Empty<SQSEvent.SQSMessage>())
        {
            await AsyncWorkload.Suspend();

            var message = JsonSerializer.Deserialize<SqsBenchmarkMessage>(record.Body)!;
            _ = SqsWorkload.Execute(message);
        }

        return new SQSBatchResponse
        {
            BatchItemFailures = []
        };
    }
}

internal static class SqsEnvelopeFactory
{
    private static readonly int[] BatchSizes = [1, 10, 100];

    public static IReadOnlyDictionary<int, SQSEvent> Create() =>
        BatchSizes.ToDictionary(batchSize => batchSize, CreateEnvelope);

    private static SQSEvent CreateEnvelope(int batchSize) =>
        new()
        {
            Records = Enumerable.Range(0, batchSize)
                .Select(index => new SQSEvent.SQSMessage
                {
                    MessageId = $"message-{index}",
                    Body = SqsWorkload.Body
                })
                .ToList()
        };
}
