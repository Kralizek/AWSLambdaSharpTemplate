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
    protected override void RegisterFrameworkServices(IServiceCollection services)
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

        var context = CreateRecordContext(envelope, lambdaContext);

        await using var invocationScope = ServiceProvider.CreateAsyncScope();

        var results = await ProcessRecordsAsync(
            envelope,
            context,
            invocationScope.ServiceProvider,
            CancellationToken.None).ConfigureAwait(false);

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
    /// Determines whether a source-specific record result represents successful processing.
    /// </summary>
    /// <remarks>
    /// Returning <see langword="false"/> marks the record activity as failed and records the low-cardinality
    /// framework metric outcome as <c>failure</c>. Exceptions remain a distinct <c>error</c> outcome.
    /// </remarks>
    protected virtual bool IsSuccessfulRecordResult(TRecordResult result) => true;

    /// <summary>
    /// Adds source-specific result metadata to the activity after a record handler returns.
    /// </summary>
    /// <remarks>
    /// Result metadata should use bounded values. Application-provided failure messages or other arbitrary text
    /// should not be copied to framework telemetry.
    /// </remarks>
    protected virtual void EnrichRecordResultActivity(Activity activity, TRecordResult result)
    {
    }

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
        TContext context,
        CancellationToken cancellationToken) =>
        ValueTask.FromException<TRecordResult>(exception);

    /// <summary>
    /// Processes all records sequentially and creates one scope per record.
    /// </summary>
    /// <remarks>
    /// Source-specific specializations may override this method to select another scheduling policy,
    /// such as bounded parallelism, while retaining the invocation scope owned by <see cref="FunctionHandlerAsync"/>.
    /// </remarks>
    protected virtual async Task<IReadOnlyCollection<RecordProcessingResult>> ProcessRecordsAsync(
        TEnvelope envelope,
        TContext context,
        IServiceProvider invocationServices,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var records = GetRecords(envelope);
        var results = records.TryGetNonEnumeratedCount(out var recordCount)
            ? new List<RecordProcessingResult>(recordCount)
            : new List<RecordProcessingResult>();
        var processor = invocationServices.GetRequiredService<IRecordProcessor<TRecord, TRecordResult, TContext>>();

        foreach (var record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await ExecuteRecordAsync(
                processor,
                record,
                context,
                cancellationToken).ConfigureAwait(false);

            results.Add(new RecordProcessingResult(record, result));
        }

        return results;
    }

    /// <summary>
    /// Processes records with bounded parallelism and creates one scope per record.
    /// </summary>
    protected async Task<IReadOnlyCollection<RecordProcessingResult>> ProcessRecordsParallelAsync(
        TEnvelope envelope,
        TContext context,
        IServiceProvider invocationServices,
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

        var processor = invocationServices.GetRequiredService<IRecordProcessor<TRecord, TRecordResult, TContext>>();
        var records = GetRecords(envelope).ToArray();
        var results = new RecordProcessingResult[records.Length];

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
                var result = await ExecuteRecordAsync(
                    processor,
                    record,
                    context,
                    ct).ConfigureAwait(false);

                results[index] = new RecordProcessingResult(record, result);
            }).ConfigureAwait(false);

        return results;
    }

    private async ValueTask<TRecordResult> ExecuteRecordAsync(
        IRecordProcessor<TRecord, TRecordResult, TContext> processor,
        TRecord record,
        TContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            return await processor.ProcessAsync(record, context, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var result = await HandleRecordExceptionAsync(
                record,
                exception,
                context,
                cancellationToken).ConfigureAwait(false);

            return result ?? throw new InvalidOperationException(
                $"Record exception handler for {typeof(THandler).Name} returned a null result.");
        }
    }

    /// <summary>
    /// Associates an original record with the result produced while processing it.
    /// </summary>
    protected readonly record struct RecordProcessingResult(TRecord Record, TRecordResult Result);
}
