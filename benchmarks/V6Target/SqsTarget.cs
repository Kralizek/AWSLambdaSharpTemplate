using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.TestUtilities;

using BenchmarkWorkloads;

using Kralizek.Lambda;

namespace V6Target;

public sealed class UppercaseRawSqsTarget : ISqsTarget
{
    private readonly UppercaseRawSqsFunction _function = new();
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

public sealed class UppercaseTypedSqsTarget : ISqsTarget
{
    private readonly UppercaseTypedSqsFunction _function = new();
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

public sealed class UppercaseAsyncRawSqsTarget : ISqsTarget
{
    private readonly UppercaseAsyncRawSqsFunction _function = new();
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

public sealed class UppercaseAsyncTypedSqsTarget : ISqsTarget
{
    private readonly UppercaseAsyncTypedSqsFunction _function = new();
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

public sealed class UppercaseRawSqsFunction : SqsFunction<UppercaseRawSqsHandler>;

public sealed class UppercaseRawSqsHandler : ISqsRecordHandler
{
    public ValueTask<SqsRecordResult> HandleAsync(
        SQSEvent.SQSMessage record,
        SqsMessageContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var message = JsonSerializer.Deserialize<SqsBenchmarkMessage>(record.Body)!;
        _ = SqsWorkload.Execute(message);

        return ValueTask.FromResult(SqsRecordResult.Success);
    }
}

public sealed class UppercaseTypedSqsFunction : SqsFunction<SqsBenchmarkMessage, UppercaseTypedSqsHandler>;

public sealed class UppercaseTypedSqsHandler : ISqsMessageHandler<SqsBenchmarkMessage>
{
    public ValueTask<SqsRecordResult> HandleAsync(
        SqsBenchmarkMessage message,
        SqsMessageContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _ = SqsWorkload.Execute(message);

        return ValueTask.FromResult(SqsRecordResult.Success);
    }
}

public sealed class UppercaseAsyncRawSqsFunction : SqsFunction<UppercaseAsyncRawSqsHandler>;

public sealed class UppercaseAsyncRawSqsHandler : ISqsRecordHandler
{
    public async ValueTask<SqsRecordResult> HandleAsync(
        SQSEvent.SQSMessage record,
        SqsMessageContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await AsyncWorkload.Suspend();

        var message = JsonSerializer.Deserialize<SqsBenchmarkMessage>(record.Body)!;
        _ = SqsWorkload.Execute(message);

        return SqsRecordResult.Success;
    }
}

public sealed class UppercaseAsyncTypedSqsFunction : SqsFunction<SqsBenchmarkMessage, UppercaseAsyncTypedSqsHandler>;

public sealed class UppercaseAsyncTypedSqsHandler : ISqsMessageHandler<SqsBenchmarkMessage>
{
    public async ValueTask<SqsRecordResult> HandleAsync(
        SqsBenchmarkMessage message,
        SqsMessageContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await AsyncWorkload.Suspend();
        _ = SqsWorkload.Execute(message);

        return SqsRecordResult.Success;
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
