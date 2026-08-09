using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class S3FunctionBase<TRecordHandler>
    : RecordFunction<S3Event, S3Event.S3EventNotificationRecord, S3RecordResult, object?, RecordContext, TRecordHandler>
    where TRecordHandler : class, IRecordHandler<S3Event.S3EventNotificationRecord, S3RecordResult, RecordContext>
{
    protected override RecordContext CreateRecordContext(S3Event envelope, ILambdaContext lambdaContext) =>
        FunctionContextFactory.CreateRecordContext(lambdaContext);

    protected override IEnumerable<S3Event.S3EventNotificationRecord> GetRecords(S3Event envelope) =>
        envelope.Records ?? Enumerable.Empty<S3Event.S3EventNotificationRecord>();

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
        services.TryAddScoped<THandler>();
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
            task.Request = envelope;
        }

        return envelope.Tasks;
    }

    protected override S3BatchResponse CreateResponse(IReadOnlyCollection<RecordProcessingResult> results)
    {
        var first = results.FirstOrDefault();
        var request = first.Record?.Request ?? throw new InvalidOperationException("The S3 Batch response cannot be created without a request.");

        return new S3BatchResponse
        {
            InvocationId = request.InvocationId,
            Results = results.Select(result => new S3BatchTaskResponse
            {
                TaskId = result.Record.TaskId,
                ResultCode = result.Result.Code.ToString(),
                ResultString = result.Result.Message
            }).ToList()
        };
    }
}

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class RawS3BatchItemHandler<THandler>
    : IRecordHandler<S3BatchTask, S3BatchResult, RecordContext>
    where THandler : class, IS3BatchItemHandler
{
    private readonly THandler _handler;

    public RawS3BatchItemHandler(THandler handler) =>
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));

    public ValueTask<S3BatchResult> HandleAsync(
        S3BatchTask record,
        RecordContext context,
        CancellationToken cancellationToken) =>
        _handler.HandleAsync(
            S3BatchItem.Create(record),
            S3BatchContext.Create(context, record),
            cancellationToken);
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