using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Kralizek.Lambda;

/// <summary>
/// Registration helpers for record processors that reuse the framework record-scope semantics.
/// </summary>
public static class RecordProcessorServiceCollectionExtensions
{
    /// <summary>
    /// Registers a record handler together with a processor that creates an independent scope for every processed record.
    /// </summary>
#pragma warning disable S2436 // The generic roles mirror the record-processing model.
    public static IServiceCollection AddRecordProcessor<TRecord, TRecordResult, TContext, THandler>(this IServiceCollection services)
#pragma warning restore S2436
        where TRecordResult : LambdaRecordResult
        where TContext : RecordContext
        where THandler : class, IRecordHandler<TRecord, TRecordResult, TContext> =>
        AddRecordProcessor<TRecord, TRecordResult, TContext, THandler>(services, null, null, null);

    /// <summary>
    /// Registers a record handler together with a processor and source-owned telemetry callbacks.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
#pragma warning disable S2436 // The generic roles mirror the record-processing model.
    public static IServiceCollection AddRecordProcessor<TRecord, TRecordResult, TContext, THandler>(
#pragma warning restore S2436
        this IServiceCollection services,
        Action<Activity, TRecord, TContext>? enrichActivity,
        Func<TRecordResult, bool>? isSuccessfulResult,
        Action<Activity, TRecordResult>? enrichResultActivity)
        where TRecordResult : LambdaRecordResult
        where TContext : RecordContext
        where THandler : class, IRecordHandler<TRecord, TRecordResult, TContext>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<THandler>();
        services.TryAddSingleton<IRecordProcessor<TRecord, TRecordResult, TContext>>(serviceProvider =>
            new RecordProcessor<TRecord, TRecordResult, TContext, THandler>(
                serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                serviceProvider.GetRequiredService<ILogger<RecordProcessor<TRecord, TRecordResult, TContext, THandler>>>(),
                enrichActivity,
                isSuccessfulResult,
                enrichResultActivity));

        return services;
    }
}

#pragma warning disable S2436 // Record, result, context, and handler are distinct roles in the record-processing model.
internal sealed class RecordProcessor<TRecord, TRecordResult, TContext, THandler>
#pragma warning restore S2436
    : IRecordProcessor<TRecord, TRecordResult, TContext>
    where TRecordResult : LambdaRecordResult
    where TContext : RecordContext
    where THandler : class, IRecordHandler<TRecord, TRecordResult, TContext>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecordProcessor<TRecord, TRecordResult, TContext, THandler>> _logger;
    private readonly Action<Activity, TRecord, TContext>? _enrichActivity;
    private readonly Func<TRecordResult, bool>? _isSuccessfulResult;
    private readonly Action<Activity, TRecordResult>? _enrichResultActivity;

    public RecordProcessor(
        IServiceScopeFactory scopeFactory,
        ILogger<RecordProcessor<TRecord, TRecordResult, TContext, THandler>> logger,
        Action<Activity, TRecord, TContext>? enrichActivity = null,
        Func<TRecordResult, bool>? isSuccessfulResult = null,
        Action<Activity, TRecordResult>? enrichResultActivity = null)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _enrichActivity = enrichActivity;
        _isSuccessfulResult = isSuccessfulResult;
        _enrichResultActivity = enrichResultActivity;
    }

    public async ValueTask<TRecordResult> ProcessAsync(
        TRecord record,
        TContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var activity = LambdaTelemetry.StartRecordActivity();
        if (activity is not null)
        {
            _enrichActivity?.Invoke(activity, record, context);
        }

        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            await using var recordScope = _scopeFactory.CreateAsyncScope();
            var handler = recordScope.ServiceProvider.GetRequiredService<THandler>();

            _logger.LogDebug("Invoking handler {Handler}", typeof(THandler).Name);

            var result = await handler.HandleAsync(record, context, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException(
                    $"Record handler {typeof(THandler).Name} returned a null result.");

            var isSuccessful = _isSuccessfulResult?.Invoke(result) ?? true;

            if (activity is not null)
            {
                _enrichResultActivity?.Invoke(activity, result);

                if (!isSuccessful)
                {
                    activity.SetStatus(ActivityStatusCode.Error);
                }
            }

            LambdaTelemetry.RecordProcessed(
                isSuccessful ? "success" : "failure",
                Stopwatch.GetElapsedTime(startedAt));

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "canceled");
            LambdaTelemetry.RecordProcessed("canceled", Stopwatch.GetElapsedTime(startedAt));
            throw;
        }
        catch (Exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            LambdaTelemetry.RecordProcessed("error", Stopwatch.GetElapsedTime(startedAt));
            throw;
        }
    }
}
