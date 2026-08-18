#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.TestUtilities;

using BenchmarkWorkloads;

using Kralizek.Lambda;

using Microsoft.Extensions.DependencyInjection;

namespace V5Target;

public sealed class FailureSqsTarget : ISqsFailureTarget
{
    private readonly ReturnedFailureSqsFunction _returnedFunction = new();
    private readonly ExceptionFailureSqsFunction _exceptionFunction = new();
    private readonly ILambdaContext _context = new TestLambdaContext
    {
        RemainingTime = TimeSpan.FromMinutes(1)
    };
    private readonly IReadOnlyDictionary<int, SQSEvent> _events = FailureSqsEnvelopeFactory.Create();

    public async Task<int> InvokeAsync(int failurePercent, SqsFailureMode mode)
    {
        var response = mode switch
        {
            SqsFailureMode.ReturnedResult => await _returnedFunction.FunctionHandlerAsync(_events[failurePercent], _context).ConfigureAwait(false),
            SqsFailureMode.Exception => await _exceptionFunction.FunctionHandlerAsync(_events[failurePercent], _context).ConfigureAwait(false),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

        return response.BatchItemFailures?.Count ?? 0;
    }
}

public sealed class ReturnedFailureSqsFunction : RequestResponseFunction<SQSEvent, SQSBatchResponse>
{
    protected override void ConfigureServices(IServiceCollection services, IExecutionEnvironment executionEnvironment) =>
        RegisterHandler<ReturnedFailureSqsHandler>(services);
}

public sealed class ReturnedFailureSqsHandler : IRequestResponseHandler<SQSEvent, SQSBatchResponse>
{
    public Task<SQSBatchResponse> HandleAsync(SQSEvent? input, ILambdaContext context)
    {
        var failures = new List<SQSBatchResponse.BatchItemFailure>();

        foreach (var record in input?.Records ?? Enumerable.Empty<SQSEvent.SQSMessage>())
        {
            var message = JsonSerializer.Deserialize<SqsFailureBenchmarkMessage>(record.Body)!;
            _ = SqsFailureWorkload.Execute(message);

            if (message.ShouldFail)
            {
                failures.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = record.MessageId });
            }
        }

        return Task.FromResult(new SQSBatchResponse { BatchItemFailures = failures });
    }
}

public sealed class ExceptionFailureSqsFunction : RequestResponseFunction<SQSEvent, SQSBatchResponse>
{
    protected override void ConfigureServices(IServiceCollection services, IExecutionEnvironment executionEnvironment) =>
        RegisterHandler<ExceptionFailureSqsHandler>(services);
}

public sealed class ExceptionFailureSqsHandler : IRequestResponseHandler<SQSEvent, SQSBatchResponse>
{
    public Task<SQSBatchResponse> HandleAsync(SQSEvent? input, ILambdaContext context)
    {
        var failures = new List<SQSBatchResponse.BatchItemFailure>();

        foreach (var record in input?.Records ?? Enumerable.Empty<SQSEvent.SQSMessage>())
        {
            try
            {
                ProcessRecord(record);
            }
            catch (InvalidOperationException)
            {
                failures.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = record.MessageId });
            }
        }

        return Task.FromResult(new SQSBatchResponse { BatchItemFailures = failures });
    }

    private static void ProcessRecord(SQSEvent.SQSMessage record)
    {
        var message = JsonSerializer.Deserialize<SqsFailureBenchmarkMessage>(record.Body)!;
        _ = SqsFailureWorkload.Execute(message);

        if (message.ShouldFail)
        {
            throw new InvalidOperationException("Synthetic benchmark failure.");
        }
    }
}

internal static class FailureSqsEnvelopeFactory
{
    private static readonly int[] FailurePercentages = [0, 10, 50, 100];

    public static IReadOnlyDictionary<int, SQSEvent> Create() =>
        FailurePercentages.ToDictionary(failurePercent => failurePercent, CreateEnvelope);

    private static SQSEvent CreateEnvelope(int failurePercent) =>
        new()
        {
            Records = Enumerable.Range(0, SqsFailureWorkload.BatchSize)
                .Select(index => new SQSEvent.SQSMessage
                {
                    MessageId = $"message-{index}",
                    Body = SqsFailureWorkload.CreateBody(SqsFailureWorkload.ShouldFail(index, failurePercent))
                })
                .ToList()
        };
}
