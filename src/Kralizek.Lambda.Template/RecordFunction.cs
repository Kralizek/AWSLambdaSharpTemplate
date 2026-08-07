using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kralizek.Lambda;

/// <summary>
/// A function base class for handlers that process an envelope containing multiple records.
/// </summary>
/// <typeparam name="TEnvelope">The AWS envelope type received from the runtime.</typeparam>
/// <typeparam name="TRecord">The individual record type extracted from the envelope.</typeparam>
/// <typeparam name="TRecordResult">The result produced by processing one record.</typeparam>
/// <typeparam name="TResponse">The infrastructure response produced from the record results.</typeparam>
/// <typeparam name="THandler">The concrete handler type that processes each record.</typeparam>
#pragma warning disable S2436 // The five generic roles are intentional and mirror the record-processing model from ADR #30.
public abstract class RecordFunction<TEnvelope, TRecord, TRecordResult, TResponse, THandler> : LambdaFunction
#pragma warning restore S2436
    where THandler : class, IRecordHandler<TRecord, TRecordResult>
{
    private protected override void ConfigureFrameworkServices(IServiceCollection services)
    {
        base.ConfigureFrameworkServices(services);
        services.TryAddScoped<THandler>();
    }

    /// <summary>
    /// Extracts the individual records from the envelope.
    /// </summary>
    protected abstract IEnumerable<TRecord> GetRecords(TEnvelope envelope);

    /// <summary>
    /// Creates the final source-specific response from the processed records and their results.
    /// </summary>
    protected abstract TResponse CreateResponse(IReadOnlyCollection<RecordProcessingResult> results);

    /// <summary>
    /// Translates a record-handler exception into a source-specific record result.
    /// </summary>
    /// <remarks>
    /// The default implementation rethrows the exception. Source-specific specializations may override
    /// this method to produce a failed record result used for partial-batch or checkpoint responses.
    /// Invocation cancellation bypasses this method and always aborts the invocation.
    /// </remarks>
    protected virtual ValueTask<TRecordResult> HandleRecordExceptionAsync(
        TRecord record,
        Exception exception,
        RecordContext context,
        CancellationToken cancellationToken) =>
        ValueTask.FromException<TRecordResult>(exception);

    /// <summary>
    /// Processes all records sequentially within an invocation scope and creates one scope per record.
    /// </summary>
    protected async Task<TResponse> ProcessRecordsAsync(
        TEnvelope envelope,
        RecordContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await using var invocationScope = ServiceProvider.CreateAsyncScope();
        var results = new List<RecordProcessingResult>();

        foreach (var record in GetRecords(envelope))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await ProcessRecordAsync(
                invocationScope.ServiceProvider,
                record,
                context,
                cancellationToken).ConfigureAwait(false);

            results.Add(new RecordProcessingResult(record, result));
        }

        return CreateResponse(results);
    }

    /// <summary>
    /// Processes records with bounded parallelism within an invocation scope and creates one scope per record.
    /// </summary>
    protected async Task<TResponse> ProcessRecordsParallelAsync(
        TEnvelope envelope,
        RecordContext context,
        int maxDegreeOfParallelism,
        CancellationToken cancellationToken)
    {
        if (maxDegreeOfParallelism < 2)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxDegreeOfParallelism),
                "maxDegreeOfParallelism must be at least 2.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var records = GetRecords(envelope).ToArray();
        var results = new RecordProcessingResult[records.Length];

        await using var invocationScope = ServiceProvider.CreateAsyncScope();

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(
            Enumerable.Range(0, records.Length),
            options,
            async (index, ct) =>
            {
                var record = records[index];
                var result = await ProcessRecordAsync(
                    invocationScope.ServiceProvider,
                    record,
                    context,
                    ct).ConfigureAwait(false);

                results[index] = new RecordProcessingResult(record, result);
            }).ConfigureAwait(false);

        return CreateResponse(results);
    }

    /// <summary>
    /// Creates the common record-processing context for an invocation.
    /// </summary>
    protected static RecordContext CreateRecordContext(ILambdaContext context) => new(context);

    private async ValueTask<TRecordResult> ProcessRecordAsync(
        IServiceProvider invocationServices,
        TRecord record,
        RecordContext context,
        CancellationToken cancellationToken)
    {
        await using var recordScope = invocationServices.CreateAsyncScope();

        try
        {
            return await InvokeHandlerAsync<THandler, TRecordResult>(
                recordScope.ServiceProvider,
                cancellationToken,
                (handler, ct) => handler.HandleAsync(record, context, ct)).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return await HandleRecordExceptionAsync(
                record,
                exception,
                context,
                cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Associates an original record with the result produced while processing it.
    /// </summary>
    protected readonly record struct RecordProcessingResult(TRecord Record, TRecordResult Result);
}

/// <summary>
/// The contract for handlers invoked by <see cref="RecordFunction{TEnvelope,TRecord,TRecordResult,TResponse,THandler}"/>.
/// </summary>
/// <typeparam name="TRecord">The individual record type to handle.</typeparam>
/// <typeparam name="TRecordResult">The result produced from processing the record.</typeparam>
public interface IRecordHandler<in TRecord, TRecordResult>
{
    ValueTask<TRecordResult> HandleAsync(TRecord record, RecordContext context, CancellationToken cancellationToken);
}