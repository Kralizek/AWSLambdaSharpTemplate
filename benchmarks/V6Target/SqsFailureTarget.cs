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

using Microsoft.Extensions.Logging;

namespace V6Target;

public sealed class FailureMinimalSqsTarget : ISqsFailureTarget
{
    private readonly ReturnedFailureMinimalSqsFunction _returnedFunction = new();
    private readonly ExceptionFailureMinimalSqsFunction _exceptionFunction = new();
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

public sealed class FailureRawSqsTarget : ISqsFailureTarget
{
    private readonly ReturnedFailureRawSqsFunction _returnedFunction = new();
    private readonly ExceptionFailureRawSqsFunction _exceptionFunction = new();
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

public sealed class FailureTypedSqsTarget : ISqsFailureTarget
{
    private readonly ReturnedFailureTypedSqsFunction _returnedFunction = new();
    private readonly ExceptionFailureTypedSqsFunction _exceptionFunction = new();
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

public sealed class ReturnedFailureMinimalSqsFunction : MinimalRequestFunction<SQSEvent, SQSBatchResponse, ReturnedFailureMinimalSqsHandler>;

public sealed class ReturnedFailureMinimalSqsHandler : IRequestHandler<SQSEvent, SQSBatchResponse>
{
    public ValueTask<SQSBatchResponse> HandleAsync(
        SQSEvent input,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var failures = new List<SQSBatchResponse.BatchItemFailure>();

        foreach (var record in input.Records ?? Enumerable.Empty<SQSEvent.SQSMessage>())
        {
            cancellationToken.ThrowIfCancellationRequested();

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

        return ValueTask.FromResult(new SQSBatchResponse
        {
            BatchItemFailures = failures
        });
    }
}

public sealed class ExceptionFailureMinimalSqsFunction : MinimalRequestFunction<SQSEvent, SQSBatchResponse, ExceptionFailureMinimalSqsHandler>;

public sealed class ExceptionFailureMinimalSqsHandler : IRequestHandler<SQSEvent, SQSBatchResponse>
{
    public ValueTask<SQSBatchResponse> HandleAsync(
        SQSEvent input,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var failures = new List<SQSBatchResponse.BatchItemFailure>();

        foreach (var record in input.Records ?? Enumerable.Empty<SQSEvent.SQSMessage>())
        {
            cancellationToken.ThrowIfCancellationRequested();

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

        return ValueTask.FromResult(new SQSBatchResponse
        {
            BatchItemFailures = failures
        });
    }
}

public sealed class ReturnedFailureRawSqsFunction : SqsFunction<ReturnedFailureRawSqsHandler>;

public sealed class ReturnedFailureRawSqsHandler : ISqsRecordHandler
{
    public ValueTask<SqsRecordResult> HandleAsync(
        SQSEvent.SQSMessage record,
        SqsMessageContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var message = JsonSerializer.Deserialize<SqsFailureBenchmarkMessage>(record.Body)
            ?? throw new JsonException("The benchmark SQS message could not be deserialized.");
        _ = SqsFailureWorkload.Execute(message);

        return ValueTask.FromResult(
            message.ShouldFail
                ? SqsRecordResult.Failed("benchmark failure")
                : SqsRecordResult.Success);
    }
}

public sealed class ExceptionFailureRawSqsFunction : SqsFunction<ExceptionFailureRawSqsHandler>
{
    protected override void ConfigureLogging(ILoggingBuilder logging) =>
        logging.ClearProviders();
}

public sealed class ExceptionFailureRawSqsHandler : ISqsRecordHandler
{
    public ValueTask<SqsRecordResult> HandleAsync(
        SQSEvent.SQSMessage record,
        SqsMessageContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var message = JsonSerializer.Deserialize<SqsFailureBenchmarkMessage>(record.Body)
            ?? throw new JsonException("The benchmark SQS message could not be deserialized.");
        _ = SqsFailureWorkload.Execute(message);

        if (message.ShouldFail)
        {
            throw new InvalidOperationException("benchmark failure");
        }

        return ValueTask.FromResult(SqsRecordResult.Success);
    }
}

public sealed class ReturnedFailureTypedSqsFunction : SqsFunction<SqsFailureBenchmarkMessage, ReturnedFailureTypedSqsHandler>;

public sealed class ReturnedFailureTypedSqsHandler : ISqsMessageHandler<SqsFailureBenchmarkMessage>
{
    public ValueTask<SqsRecordResult> HandleAsync(
        SqsFailureBenchmarkMessage message,
        SqsMessageContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = SqsFailureWorkload.Execute(message);

        return ValueTask.FromResult(
            message.ShouldFail
                ? SqsRecordResult.Failed("benchmark failure")
                : SqsRecordResult.Success);
    }
}

public sealed class ExceptionFailureTypedSqsFunction : SqsFunction<SqsFailureBenchmarkMessage, ExceptionFailureTypedSqsHandler>
{
    protected override void ConfigureLogging(ILoggingBuilder logging) =>
        logging.ClearProviders();
}

public sealed class ExceptionFailureTypedSqsHandler : ISqsMessageHandler<SqsFailureBenchmarkMessage>
{
    public ValueTask<SqsRecordResult> HandleAsync(
        SqsFailureBenchmarkMessage message,
        SqsMessageContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = SqsFailureWorkload.Execute(message);

        if (message.ShouldFail)
        {
            throw new InvalidOperationException("benchmark failure");
        }

        return ValueTask.FromResult(SqsRecordResult.Success);
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
