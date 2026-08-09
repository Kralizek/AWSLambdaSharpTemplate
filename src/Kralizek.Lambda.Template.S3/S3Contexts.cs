using System;
using System.Collections.Generic;

using Amazon.Lambda.S3Events;

namespace Kralizek.Lambda;

public sealed class S3RecordContext : RecordContext
{
    private S3RecordContext(FunctionContextMetadata metadata, IReadOnlyDictionary<string, object?> properties)
        : base(metadata, properties)
    {
    }

    internal static S3RecordContext Create(RecordContext invocationContext, S3Event.S3EventNotificationRecord record)
    {
        ArgumentNullException.ThrowIfNull(invocationContext);
        ArgumentNullException.ThrowIfNull(record);

        var metadata = CopyMetadata(invocationContext);
        var properties = new Dictionary<string, object?>(invocationContext.Properties)
        {
            [S3RecordContextExtensions.S3RecordPropertyName] = record
        };

        return new S3RecordContext(metadata, properties);
    }

    internal static FunctionContextMetadata CopyMetadata(RecordContext context) =>
        new(
            context.AwsRequestId,
            context.FunctionName,
            context.FunctionVersion,
            context.InvokedFunctionArn,
            context.MemoryLimitInMB,
            context.RemainingTime,
            context.LogGroupName,
            context.LogStreamName);
}

public static class S3RecordContextExtensions
{
    internal const string S3RecordPropertyName = "Kralizek.Lambda.Template.S3.S3Record";

    public static S3Event.S3EventNotificationRecord GetS3EventRecord(this S3RecordContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Properties.TryGetValue(S3RecordPropertyName, out var value) && value is S3Event.S3EventNotificationRecord record)
        {
            return record;
        }

        throw new InvalidOperationException("The S3 record context does not contain an AWS S3 event record.");
    }
}

public sealed class S3BatchContext : RecordContext
{
    private S3BatchContext(
        FunctionContextMetadata metadata,
        IReadOnlyDictionary<string, object?> properties,
        S3BatchEvent request,
        S3BatchTask task)
        : base(metadata, properties)
    {
        InvocationId = request.InvocationId ?? string.Empty;
        JobId = request.Job?.Id ?? string.Empty;
        TaskId = task.TaskId ?? string.Empty;
        UserArguments = request.Job?.UserArguments is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(request.Job.UserArguments);
    }

    public string InvocationId { get; }
    public string JobId { get; }
    public string TaskId { get; }
    public IReadOnlyDictionary<string, string> UserArguments { get; }

    internal static S3BatchContext Create(RecordContext invocationContext, S3BatchTask task)
    {
        ArgumentNullException.ThrowIfNull(invocationContext);
        ArgumentNullException.ThrowIfNull(task);

        var request = task.Request ?? throw new InvalidOperationException("The S3 Batch task is not associated with its invocation request.");
        var properties = new Dictionary<string, object?>(invocationContext.Properties)
        {
            [S3BatchContextExtensions.S3BatchRequestPropertyName] = request,
            [S3BatchContextExtensions.S3BatchTaskPropertyName] = task
        };

        return new S3BatchContext(S3RecordContext.CopyMetadata(invocationContext), properties, request, task);
    }
}

public static class S3BatchContextExtensions
{
    internal const string S3BatchRequestPropertyName = "Kralizek.Lambda.Template.S3.S3BatchRequest";
    internal const string S3BatchTaskPropertyName = "Kralizek.Lambda.Template.S3.S3BatchTask";

    public static S3BatchEvent GetS3BatchRequest(this S3BatchContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Properties.TryGetValue(S3BatchRequestPropertyName, out var value) && value is S3BatchEvent request)
        {
            return request;
        }

        throw new InvalidOperationException("The S3 Batch context does not contain an S3 Batch request.");
    }

    public static S3BatchTask GetS3BatchTask(this S3BatchContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Properties.TryGetValue(S3BatchTaskPropertyName, out var value) && value is S3BatchTask task)
        {
            return task;
        }

        throw new InvalidOperationException("The S3 Batch context does not contain an S3 Batch task.");
    }
}