using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Kralizek.Lambda;

/// <summary>
/// Infrastructure base for SQS function specializations.
/// </summary>
/// <typeparam name="TRecordHandler">The infrastructure record handler used by the specialization.</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class SqsFunctionBase<TRecordHandler>
    : RecordFunction<
        SQSEvent,
        SQSEvent.SQSMessage,
        SqsRecordResult,
        SQSBatchResponse,
        RecordContext,
        TRecordHandler>
    where TRecordHandler : class, IRecordHandler<SQSEvent.SQSMessage, SqsRecordResult, RecordContext>
{
    protected override RecordContext CreateRecordContext(SQSEvent envelope, ILambdaContext lambdaContext) =>
        FunctionContextFactory.CreateRecordContext(lambdaContext);

    protected override IEnumerable<SQSEvent.SQSMessage> GetRecords(SQSEvent envelope) =>
        envelope.Records ?? Enumerable.Empty<SQSEvent.SQSMessage>();

    protected override void EnrichRecordActivity(Activity activity, SQSEvent.SQSMessage record, RecordContext context)
    {
        activity.SetTag("messaging.system", "aws.sqs");
        activity.SetTag("messaging.operation.name", "process");
        activity.SetTag("messaging.operation.type", "process");
        activity.SetTag("messaging.message.id", record.MessageId);

        if (!string.IsNullOrWhiteSpace(record.EventSourceArn))
        {
            activity.SetTag("cloud.resource_id", record.EventSourceArn);
            activity.SetTag("messaging.destination.name", GetResourceName(record.EventSourceArn));
        }
    }

    protected override bool IsSuccessfulRecordResult(SqsRecordResult result) =>
        result.Value is SqsRecordResult.SuccessCase;

    protected override SQSBatchResponse CreateResponse(IReadOnlyCollection<RecordProcessingResult> results)
    {
        var failures = results
            .Where(result => result.Result.Value is SqsRecordResult.FailureCase)
            .Select(result => new SQSBatchResponse.BatchItemFailure
            {
                ItemIdentifier = result.Record.MessageId
            })
            .ToList();

        return new SQSBatchResponse
        {
            BatchItemFailures = failures
        };
    }

    protected override ValueTask<SqsRecordResult> HandleRecordExceptionAsync(
        SQSEvent.SQSMessage record,
        Exception exception,
        RecordContext context,
        CancellationToken cancellationToken)
    {
        Logger.LogError(exception, "Failed to process SQS record {MessageId}", record.MessageId);
        return ValueTask.FromResult(SqsRecordResult.Failed(exception.Message));
    }

    private static string GetResourceName(string arn)
    {
        var separator = arn.LastIndexOf(':');
        return separator >= 0 && separator < arn.Length - 1 ? arn[(separator + 1)..] : arn;
    }
}

/// <summary>
/// Infrastructure base for SQS functions that process records with bounded parallelism.
/// </summary>
/// <typeparam name="TRecordHandler">The infrastructure record handler used by the specialization.</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class ParallelSqsFunctionBase<TRecordHandler> : SqsFunctionBase<TRecordHandler>
    where TRecordHandler : class, IRecordHandler<SQSEvent.SQSMessage, SqsRecordResult, RecordContext>
{
    /// <summary>
    /// Gets the maximum number of SQS records processed concurrently.
    /// </summary>
    protected virtual int MaxDegreeOfParallelism => Math.Max(2, Environment.ProcessorCount);

    protected override Task<IReadOnlyCollection<RecordProcessingResult>> ProcessRecordsAsync(
        SQSEvent envelope,
        RecordContext context,
        IServiceProvider invocationServices,
        CancellationToken cancellationToken) =>
        ProcessRecordsParallelAsync(
            envelope,
            context,
            invocationServices,
            MaxDegreeOfParallelism,
            cancellationToken);
}

internal static class SqsServiceRegistration
{
    public static void AddRawHandler<THandler>(IServiceCollection services)
        where THandler : class, ISqsRecordHandler =>
        services.TryAddScoped<THandler>();

    public static void AddDecodedHandler<TMessage, THandler>(IServiceCollection services)
        where THandler : class, ISqsMessageHandler<TMessage>
    {
        services.TryAddScoped<THandler>();
        services.TryAddSingleton<IStringPayloadDecoder<TMessage>, JsonStringPayloadDecoder<TMessage>>();
    }
}
