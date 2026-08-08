using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kralizek.Lambda;

/// <summary>
/// An SQS function specialization that processes raw records with bounded parallelism.
/// </summary>
/// <typeparam name="THandler">The concrete handler type that processes each SQS record.</typeparam>
public abstract class ParallelSqsFunction<THandler> : SqsFunction<THandler>
    where THandler : class, ISqsRecordHandler
{
    /// <summary>
    /// Gets the maximum number of SQS records processed concurrently.
    /// </summary>
    protected virtual int MaxDegreeOfParallelism => Math.Max(2, Environment.ProcessorCount);

    protected override Task<IReadOnlyCollection<RecordProcessingResult>> ProcessRecordsAsync(
        Amazon.Lambda.SQSEvents.SQSEvent envelope,
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

/// <summary>
/// An SQS function specialization that processes decoded messages with bounded parallelism.
/// </summary>
/// <typeparam name="TMessage">The decoded message type.</typeparam>
/// <typeparam name="THandler">The concrete handler type that processes each message.</typeparam>
public abstract class ParallelSqsFunction<TMessage, THandler> : SqsFunction<TMessage, THandler>
    where THandler : class, ISqsMessageHandler<TMessage>
{
    /// <summary>
    /// Gets the maximum number of SQS records processed concurrently.
    /// </summary>
    protected virtual int MaxDegreeOfParallelism => Math.Max(2, Environment.ProcessorCount);

    protected override Task<IReadOnlyCollection<RecordProcessingResult>> ProcessRecordsAsync(
        Amazon.Lambda.SQSEvents.SQSEvent envelope,
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