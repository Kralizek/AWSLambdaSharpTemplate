using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kralizek.Lambda;

public interface IS3ObjectEventHandler
{
    ValueTask HandleAsync(S3ObjectEvent item, S3RecordContext context, CancellationToken cancellationToken);
}

public interface IS3BatchItemHandler
{
    ValueTask<S3BatchResult> HandleAsync(S3BatchItem item, S3BatchContext context, CancellationToken cancellationToken);
}

/// <summary>
/// Registration helpers for composing S3 object-event record processing outside a direct S3 Lambda invocation.
/// </summary>
public static class S3ServiceCollectionExtensions
{
    /// <summary>
    /// Registers an S3 object-event handler and the record processor that adapts AWS S3 records to the public S3 programming model.
    /// </summary>
    public static IServiceCollection AddS3ObjectEventProcessing<THandler>(this IServiceCollection services)
        where THandler : class, IS3ObjectEventHandler
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<THandler>();
        services.AddRecordProcessor<
            S3Event.S3EventNotificationRecord,
            S3RecordResult,
            RecordContext,
            RawS3ObjectEventHandler<THandler>>(
                static (activity, record, _) => S3Telemetry.EnrichObjectEvent(activity, record),
                null,
                null);

        return services;
    }
}

[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class S3FunctionBase<TRecordHandler>
    : RecordFunction<S3Event, S3Event.S3EventNotificationRecord, S3RecordResult, object?, RecordContext, TRecordHandler>
    where TRecordHandler : class, IRecordHandler<S3Event.S3EventNotificationRecord, S3RecordResult, RecordContext>
{
    protected override RecordContext CreateRecordContext(S3Event envelope, ILambdaContext lambdaContext) =>
        FunctionContextFactory.CreateRecordContext(lambdaContext);

    protected override IEnumerable<S3Event.S3EventNotificationRecord> GetRecords(S3Event envelope) =>
        envelope.Records ?? Enumerable.Empty<S3Event.S3EventNotificationRecord>();

    protected override void EnrichRecordActivity(
        Activity activity,
        S3Event.S3EventNotificationRecord record,
        RecordContext context) =>
        S3Telemetry.EnrichObjectEvent(activity, record);

    protected override object? CreateResponse(IReadOnlyCollection<RecordProcessingResult> results) => null;
}

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class RawS3ObjectEventHandler<THandler>
    : IRecordHandler<S3Event.S3EventNotificationRecord, S3RecordResult, RecordContext>
    where THandler : class, IS3ObjectEventHandler
{
    private readonly THandler _handler;

    public RawS3ObjectEventHandler(THandler handler) =>
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));

    public async ValueTask<S3RecordResult> HandleAsync(
        S3Event.S3EventNotificationRecord record,
        RecordContext context,
        CancellationToken cancellationToken)
    {
        await _handler.HandleAsync(
            S3ObjectEvent.Create(record),
            S3RecordContext.Create(context, record),
            cancellationToken).ConfigureAwait(false);

        return S3RecordResult.Completed;
    }
}

public abstract class S3Function<THandler>
    : S3FunctionBase<RawS3ObjectEventHandler<THandler>>
    where THandler : class, IS3ObjectEventHandler
{
    protected override void ConfigureFrameworkServices(IServiceCollection services)
    {
        base.ConfigureFrameworkServices(services);
        services.AddS3ObjectEventProcessing<THandler>();
    }
}

[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class S3BatchFunctionBase<TRecordHandler>
    : RecordFunction<S3BatchEvent, S3BatchTask, S3BatchResult, S3BatchResponse, RecordContext, TRecordHandler>
    where TRecordHandler : class, IRecordHandler<S3BatchTask, S3BatchResult, RecordContext>
{
    protected override RecordContext CreateRecordContext(S3BatchEvent envelope, ILambdaContext lambdaContext) =>
        FunctionContextFactory.CreateRecordContext(lambdaContext);

    protected override IEnumerable<S3BatchTask> GetRecords(S3BatchEvent envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (!string.Equals(envelope.InvocationSchemaVersion, "2.0", StringComparison.Ordinal))
        {
            throw new NotSupportedException(
                $"S3 Batch invocation schema '{envelope.InvocationSchemaVersion ?? "<null>"}' is not supported. Configure the S3 Batch job to use schema 2.0.");
        }

        if (string.IsNullOrWhiteSpace(envelope.InvocationId))
        {
            throw new InvalidOperationException("The S3 Batch request does not contain an invocation identifier.");
        }

        if (envelope.Tasks is not { Count: > 0 })
        {
            throw new InvalidOperationException("The S3 Batch request does not contain any tasks.");
        }

        foreach (var task in envelope.Tasks)
        {
            if (string.IsNullOrWhiteSpace(task.TaskId))
            {
                throw new InvalidOperationException("The S3 Batch task does not contain a task identifier.");
            }

            task.Request = envelope;
        }

        return envelope.Tasks;
    }

    protected override void EnrichRecordActivity(Activity activity, S3BatchTask record, RecordContext context) =>
        S3Telemetry.EnrichBatchTask(activity, record);

    protected override bool IsSuccessfulRecordResult(S3BatchResult result) =>
        result.Value is S3BatchResult.SucceededCase;

    protected override void EnrichRecordResultActivity(Activity activity, S3BatchResult result) =>
        activity.SetTag(
            "kralizek.aws.s3.batch.result",
            result.Value switch
            {
                S3BatchResult.SucceededCase => "succeeded",
                S3BatchResult.TemporaryFailureCase => "temporary_failure",
                S3BatchResult.PermanentFailureCase => "permanent_failure",
                _ => "unknown"
            });

    protected override S3BatchResponse CreateResponse(IReadOnlyCollection<RecordProcessingResult> results)
    {
        var first = results.FirstOrDefault();
        var request = first.Record?.Request ?? throw new InvalidOperationException("The S3 Batch response cannot be created without a request.");

        return new S3BatchResponse
        {
            InvocationId = request.InvocationId,
            Results = results.Select(CreateTaskResponse).ToList()
        };
    }

    private static S3BatchTaskResponse CreateTaskResponse(RecordProcessingResult result)
    {
        var (code, message) = result.Result.Value switch
        {
            S3BatchResult.SucceededCase succeeded => (S3BatchResultCode.Succeeded, succeeded.Message),
            S3BatchResult.TemporaryFailureCase temporaryFailure => (S3BatchResultCode.TemporaryFailure, temporaryFailure.Message),
            S3BatchResult.PermanentFailureCase permanentFailure => (S3BatchResultCode.PermanentFailure, permanentFailure.Message),
            _ => throw new InvalidOperationException("The S3 Batch result contains an unsupported result case.")
        };

        return new S3BatchTaskResponse
        {
            TaskId = result.Record.TaskId,
            ResultCode = code.ToString(),
            ResultString = message
        };
    }
}

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class RawS3BatchItemHandler<THandler>
    : IRecordHandler<S3BatchTask, S3BatchResult, RecordContext>
    where THandler : class, IRecordHandler<S3BatchTask, S3BatchResult, RecordContext>
{
    private readonly THandler _handler;

    public RawS3BatchItemHandler(THandler handler) =>
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));

    public ValueTask<S3BatchResult> HandleAsync(
        S3BatchTask record,
        RecordContext context,
        CancellationToken cancellationToken) =>
        _handler.HandleAsync(record, context, cancellationToken);
}

public abstract class S3BatchFunction<THandler>
    : S3BatchFunctionBase<RawS3BatchItemHandler<THandler>>
    where THandler : class, IS3BatchItemHandler
{
    protected override void ConfigureFrameworkServices(IServiceCollection services)
    {
        base.ConfigureFrameworkServices(services);
        services.TryAddScoped<THandler>();
    }
}

internal static class S3Telemetry
{
    public static void EnrichObjectEvent(Activity activity, S3Event.S3EventNotificationRecord record)
    {
        activity.SetTag("aws.s3.bucket", record.S3?.Bucket?.Name);
        activity.SetTag("aws.s3.key", record.S3?.Object?.KeyDecoded);
        activity.SetTag("kralizek.aws.s3.event_name", record.EventName);
        activity.SetTag("kralizek.aws.s3.sequencer", record.S3?.Object?.Sequencer);
        activity.SetTag("cloud.region", record.AwsRegion);
    }

    public static void EnrichBatchTask(Activity activity, S3BatchTask task)
    {
        activity.SetTag("aws.s3.bucket", task.S3Bucket);
        activity.SetTag("aws.s3.key", task.S3Key is null ? null : WebUtility.UrlDecode(task.S3Key));
        activity.SetTag("kralizek.aws.s3.version_id", task.S3VersionId);
        activity.SetTag("kralizek.aws.s3.batch.task_id", task.TaskId);
    }
}
