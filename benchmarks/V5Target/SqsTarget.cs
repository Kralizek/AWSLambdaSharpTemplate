#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.TestUtilities;

using BenchmarkWorkloads;

using Kralizek.Lambda;

using Microsoft.Extensions.DependencyInjection;

namespace V5Target;

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
        await _function.FunctionHandlerAsync(_events[batchSize], _context).ConfigureAwait(false);
        return 0;
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
        await _function.FunctionHandlerAsync(_events[batchSize], _context).ConfigureAwait(false);
        return 0;
    }
}

public sealed class UppercaseSqsFunction : EventFunction<SQSEvent>
{
    protected override void ConfigureServices(IServiceCollection services, IExecutionEnvironment executionEnvironment) =>
        services.UseQueueMessageHandler<SqsBenchmarkMessage, UppercaseSqsHandler>();
}

public sealed class UppercaseSqsHandler : IMessageHandler<SqsBenchmarkMessage>
{
    public Task HandleAsync(SqsBenchmarkMessage? message, ILambdaContext context)
    {
        _ = SqsWorkload.Execute(message!);
        return Task.CompletedTask;
    }
}

public sealed class UppercaseAsyncSqsFunction : EventFunction<SQSEvent>
{
    protected override void ConfigureServices(IServiceCollection services, IExecutionEnvironment executionEnvironment) =>
        services.UseQueueMessageHandler<SqsBenchmarkMessage, UppercaseAsyncSqsHandler>();
}

public sealed class UppercaseAsyncSqsHandler : IMessageHandler<SqsBenchmarkMessage>
{
    public async Task HandleAsync(SqsBenchmarkMessage? message, ILambdaContext context)
    {
        await AsyncWorkload.Suspend();
        _ = SqsWorkload.Execute(message!);
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
