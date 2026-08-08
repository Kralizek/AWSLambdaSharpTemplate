using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        bool,
        SQSBatchResponse,
        RecordContext,
        TRecordHandler>
    where TRecordHandler : class, IRecordHandler<SQSEvent.SQSMessage, bool, RecordContext>
{
    protected override RecordContext CreateRecordContext(SQSEvent envelope, ILambdaContext lambdaContext) =>
        FunctionContextFactory.CreateRecordContext(lambdaContext);

    protected override IEnumerable<SQSEvent.SQSMessage> GetRecords(SQSEvent envelope) => envelope.Records;

    protected override SQSBatchResponse CreateResponse(IReadOnlyCollection<RecordProcessingResult> results)
    {
        var failures = results
            .Where(result => !result.Result)
            .Select(result => new SQSBatchResponse.BatchItemFailure
            {
                ItemIdentifier = result.Record.MessageId
            })
            .ToList();

        return new SQSBatchResponse(failures);
    }

    protected override ValueTask<bool> HandleRecordExceptionAsync(
        SQSEvent.SQSMessage record,
        Exception exception,
        RecordContext context,
        CancellationToken cancellationToken)
    {
        Logger.LogError(exception, "Failed to process SQS record {MessageId}", record.MessageId);
        return ValueTask.FromResult(false);
    }
}

/// <summary>
/// Infrastructure base for SQS functions that process records with bounded parallelism.
/// </summary>
/// <typeparam name="TRecordHandler">The infrastructure record handler used by the specialization.</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class ParallelSqsFunctionBase<TRecordHandler> : SqsFunctionBase<TRecordHandler>
    where TRecordHandler : class, IRecordHandler<SQSEvent.SQSMessage, bool, RecordContext>
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