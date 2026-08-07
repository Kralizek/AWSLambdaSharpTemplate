using System;
using System.Collections.Generic;
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
        services.TryAddTransient<THandler>();
    }

    /// <summary>
    /// Extracts the individual records from the envelope.
    /// </summary>
    protected abstract IEnumerable<TRecord> GetRecords(TEnvelope envelope);

    /// <summary>
    /// Creates the final source-specific response from the processed record results.
    /// </summary>
    protected abstract TResponse CreateResponse(IReadOnlyCollection<TRecordResult> results);

    /// <summary>
    /// Processes all records sequentially, one per dependency-injection scope.
    /// </summary>
    protected async Task<TResponse> ProcessRecordsAsync(
        TEnvelope envelope,
        RecordContext context,
        CancellationToken cancellationToken)
    {
        var results = new List<TRecordResult>();

        foreach (var record in GetRecords(envelope))
        {
            var result = await InvokeAsync<THandler, TRecordResult>(
                cancellationToken,
                (handler, ct) => handler.HandleAsync(record, context, ct)).ConfigureAwait(false);

            results.Add(result);
        }

        return CreateResponse(results);
    }

    /// <summary>
    /// Creates the common record-processing context for an invocation.
    /// </summary>
    protected static RecordContext CreateRecordContext(ILambdaContext context) => new(context);
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