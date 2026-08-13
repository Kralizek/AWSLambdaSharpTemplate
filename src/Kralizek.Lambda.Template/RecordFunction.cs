using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;

using Microsoft.Extensions.DependencyInjection;

namespace Kralizek.Lambda;

/// <summary>
/// A function base class for handlers that process an envelope containing multiple records.
/// </summary>
/// <typeparam name="TEnvelope">The AWS envelope type received from the runtime.</typeparam>
/// <typeparam name="TRecord">The individual record type extracted from the envelope.</typeparam>
/// <typeparam name="TRecordResult">The result produced by processing one record.</typeparam>
/// <typeparam name="TResponse">The infrastructure response produced from the record results.</typeparam>
/// <typeparam name="TContext">The context passed to record handlers.</typeparam>
/// <typeparam name="THandler">The concrete handler type that processes each record.</typeparam>
#pragma warning disable S2436 // The six generic roles are intentional and mirror the record-processing model from ADR #30.
public abstract class RecordFunction<TEnvelope, TRecord, TRecordResult, TResponse, TContext, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler> : LambdaFunction
#pragma warning restore S2436
    where TRecordResult : LambdaRecordResult
    where TContext : RecordContext
    where THandler : class, IRecordHandler<TRecord, TRecordResult, TContext>
{
    protected sealed override void RegisterFrameworkServices(IServiceCollection services)
    {
        base.RegisterFrameworkServices(services);

        services.AddRecordProcessor<TRecord, TRecordResult, TContext, THandler>(
            EnrichRecordActivity,
            IsSuccessfulRecordResult,
            EnrichRecordResultActivity);

        ConfigureFrameworkServices(services);
    }

    /// <summary>
    /// Registers services required by this record-function specialization.
    /// </summary>
    protected virtual void ConfigureFrameworkServices(IServiceCollection services)
    {
    }

    /// <summary>
    /// The entry point called by the Lambda runtime.
    /// </summary>
    public virtual async Task<TResponse> FunctionHandlerAsync(TEnvelope envelope, ILambdaContext lambdaContext)
    {
        LambdaTelemetry.EnrichInvocation("record");

        using var cts = CreateCancellationTokenSource(lambdaContext);
        var context = CreateRecordContext(envelope, lambdaContext);

        await using var invocationScope = ServiceProvider.CreateAsyncScope();

        var results = await ProcessRecordsAsync(
            envelope,
            context,
            invocationScope.ServiceProvider,
            cts.Token).ConfigureAwait(false);

        return CreateResponse(results);
    }

    /// <summary>
    /// Creates the source-specific context used while processing this invocation.
    /// </summary>
    protected abstract TContext CreateRecordContext(TEnvelope envelope, ILambdaContext lambdaContext);

    /// <summary>
    /// Extracts the individual records from the envelope.
    /// </summary>
    protected abstract IEnumerable<TRecord> GetRecords(TEnvelope envelope);

    /// <summary>
    /// Adds source-specific transport or event metadata to the activity for one record.
    /// </summary>
    /// <remarks>
    /// Implementations should keep business-specific telemetry in application-owned activities and meters.
    /// High-cardinality record identifiers belong on activities and must not be copied to framework metric tags.
    /// </remarks>
    protected virtual void EnrichRecordActivity(Activity activity, TRecord record, TContext context)
    {
    }

    /// <summary>
    /// Determines whether the handler result represents successful processing.
    /// </summary>
    protected abstract bool IsSuccessfulRecordResult(TRecordResult result);

    /// <summary>
    /// Adds result metadata to the activity for one record.
    /// </summary>
    protected virtual void EnrichRecordResultActivity(Activity activity, TRecordResult result)
    {
    }

    /// <summary>
    /// Creates the source-specific Lambda response from record processing results.
    /// </summary>
    protected abstract TResponse CreateResponse(IReadOnlyCollection<RecordProcessingResult> results);

    /// <summary>
    /// Handles an exception raised while processing one record.
    /// </summary>
    protected virtual ValueTask<TRecordResult> HandleRecordExceptionAsync(
        TRecord record,
        Exception exception,
        TContext context,
        CancellationToken cancellationToken) =>
        ValueTask.FromException<TRecordResult>(exception);

    /// <summary>
    /// Processes all records using the source's default execution strategy.
    /// </summary>
    protected virtual async Task<IReadOnlyCollection<RecordProcessingResult>> ProcessRecordsAsync(
        TEnvelope envelope,
        TContext context,
        IServiceProvider invocationServices,
        CancellationToken cancellationToken)
    {
        var processor = invocationServices.GetRequiredService<IRecordProcessor<TRecord, TRecordResult, TContext>>();
        var results = new List<RecordProcessingResult>();

        foreach (var record in GetRecords(envelope))
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(await ProcessRecordAsync(processor, record, context, cancellationToken).ConfigureAwait(false));
        }

        return results;
    }

    /// <summary>
    /// Processes all records with bounded parallelism.
    /// </summary>
    protected async Task<IReadOnlyCollection<RecordProcessingResult>> ProcessRecordsParallelAsync(
        TEnvelope envelope,
        TContext context,
        IServiceProvider invocationServices,
        int maxDegreeOfParallelism,
        CancellationToken cancellationToken)
    {
        var processor = invocationServices.GetRequiredService<IRecordProcessor<TRecord, TRecordResult, TContext>>();
        var records = GetRecords(envelope).ToList();
        var results = new RecordProcessingResult[records.Count];

        await Parallel.ForEachAsync(
            Enumerable.Range(0, records.Count),
            new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            },
            async (index, token) =>
            {
                results[index] = await ProcessRecordAsync(processor, records[index], context, token).ConfigureAwait(false);
            }).ConfigureAwait(false);

        return results;
    }

    private async ValueTask<RecordProcessingResult> ProcessRecordAsync(
        IRecordProcessor<TRecord, TRecordResult, TContext> processor,
        TRecord record,
        TContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await processor.ProcessAsync(record, context, cancellationToken).ConfigureAwait(false);
            return new RecordProcessingResult(record!, result, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var result = await HandleRecordExceptionAsync(record, exception, context, cancellationToken).ConfigureAwait(false);
            return new RecordProcessingResult(record!, result, exception);
        }
    }
}
