using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Kralizek.Lambda;

/// <summary>
/// A function base class for handlers that process a batch event containing multiple records,
/// dispatching one handler invocation per record.
/// </summary>
/// <typeparam name="TEvent">The AWS batch-event type received from the runtime.</typeparam>
/// <typeparam name="TRecord">The individual record type extracted from the event.</typeparam>
/// <typeparam name="THandler">The concrete handler type that processes each record.</typeparam>
public abstract class RecordFunction<TEvent, TRecord, THandler> : LambdaFunction
    where THandler : class, IRecordHandler<TRecord>
{
    protected override void RegisterHandlers(IServiceCollection services)
    {
        services.TryAddTransient<THandler>();
    }

    /// <summary>
    /// Extracts the individual records from the batch event.
    /// </summary>
    protected abstract IEnumerable<TRecord> GetRecords(TEvent @event);

    /// <summary>
    /// Processes all records sequentially, one per dependency-injection scope.
    /// </summary>
    protected async Task ProcessRecordsAsync(TEvent @event, CancellationToken cancellationToken)
    {
        foreach (var record in GetRecords(@event))
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var scope = ServiceProvider.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<THandler>();

            Logger.LogDebug("Invoking {Handler} for record", typeof(THandler).Name);

            await handler.HandleAsync(record, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Processes all records in parallel up to <paramref name="maxDegreeOfParallelism"/>
    /// concurrent handler invocations, one per dependency-injection scope.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxDegreeOfParallelism"/> is less than 2.
    /// </exception>
    protected Task ProcessRecordsParallelAsync(TEvent @event, int maxDegreeOfParallelism, CancellationToken cancellationToken)
    {
        if (maxDegreeOfParallelism < 2)
            throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism), "maxDegreeOfParallelism must be at least 2.");

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            CancellationToken = cancellationToken
        };

        return Parallel.ForEachAsync(GetRecords(@event), options, async (record, ct) =>
        {
            using var scope = ServiceProvider.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<THandler>();

            Logger.LogDebug("Invoking {Handler} for record", typeof(THandler).Name);

            await handler.HandleAsync(record, ct).ConfigureAwait(false);
        });
    }
}

/// <summary>
/// The contract for handlers invoked by <see cref="RecordFunction{TEvent,TRecord,THandler}"/>.
/// </summary>
/// <typeparam name="TRecord">The individual record type to handle.</typeparam>
public interface IRecordHandler<in TRecord>
{
    ValueTask HandleAsync(TRecord record, CancellationToken cancellationToken);
}
