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

public sealed class FailureSqsTarget : ISqsFailureTarget
{
    private readonly ReturnedFailureSqsFunction _returnedFunction = new();
    private readonly ExceptionFailureSqsFunction _exceptionFunction = new();
    private readonly ILambdaContext _context = new TestLambdaContext
    {
        RemainingTime = TimeSpan.FromMinutes(1)
    };
    private readonly IReadOnlyDictionary<int, SQSEvent> _events = SqsFailureEnvelopeFactory.Create();

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

public sealed class ReturnedFailureSqsFunction
{
    public Task<SQSBatchResponse> FunctionHandlerAsync(SQSEvent input, ILambdaContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var failures = new List<SQSBatchResponse.BatchItemFailure>();

        foreach (var record in input.Records ?? Enumerable.Empty<SQSEvent.SQSMessage>())
        {
            var message = JsonSerializer.Deserialize<SqsFailureBenchmarkMessage>(record.Body)
                ?? throw new JsonException("The benchmark SQS message could not be deserialized.");
            _ = SqsFailureWorkload.Execute(message);

            if (message.ShouldFail)
            {
                failures.Add(new SQSBatchResponse.BatchItemFailure
                {
                    ItemIdentifier = record.MessageId
                });
            }
        }

        return Task.FromResult(new SQSBatchResponse
        {
            BatchItemFailures = failures
        });
    }
}

public sealed class ExceptionFailureSqsFunction
{
    public Task<SQSBatchResponse> FunctionHandlerAsync(SQSEvent input, ILambdaContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var failures = new List<SQSBatchResponse.BatchItemFailure>();

        foreach (var record in input.Records ?? Enumerable.Empty<SQSEvent.SQSMessage>())
        {
            try
            {
                var message = JsonSerializer.Deserialize<SqsFailureBenchmarkMessage>(record.Body)
                    ?? throw new JsonException("The benchmark SQS message could not be deserialized.");
                _ = SqsFailureWorkload.Execute(message);

                if (message.ShouldFail)
                {
                    throw new InvalidOperationException("benchmark failure");
                }
            }
            catch (InvalidOperationException)
            {
                failures.Add(new SQSBatchResponse.BatchItemFailure
                {
                    ItemIdentifier = record.MessageId
                });
            }
        }

        return Task.FromResult(new SQSBatchResponse
        {
            BatchItemFailures = failures
        });
    }
}

internal static class SqsFailureEnvelopeFactory
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
