using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Kralizek.Lambda;

/// <summary>
/// Contains the source-neutral metadata exposed for a Lambda invocation.
/// </summary>
public sealed record FunctionContextMetadata(
    string AwsRequestId,
    string FunctionName,
    string FunctionVersion,
    string InvokedFunctionArn,
    int MemoryLimitInMB,
    TimeSpan RemainingTime,
    string LogGroupName,
    string LogStreamName);

/// <summary>
/// Provides metadata about the current function invocation without depending on a source-specific runtime context.
/// </summary>
public abstract class FunctionContext
{
    protected FunctionContext(
        FunctionContextMetadata metadata,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        AwsRequestId = metadata.AwsRequestId;
        FunctionName = metadata.FunctionName;
        FunctionVersion = metadata.FunctionVersion;
        InvokedFunctionArn = metadata.InvokedFunctionArn;
        MemoryLimitInMB = metadata.MemoryLimitInMB;
        RemainingTime = metadata.RemainingTime;
        LogGroupName = metadata.LogGroupName;
        LogStreamName = metadata.LogStreamName;

        var propertySnapshot = properties is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(properties);

        Properties = new ReadOnlyDictionary<string, object?>(propertySnapshot);
    }

    public string AwsRequestId { get; }

    public string FunctionName { get; }

    public string FunctionVersion { get; }

    public string InvokedFunctionArn { get; }

    public int MemoryLimitInMB { get; }

    public TimeSpan RemainingTime { get; }

    public string LogGroupName { get; }

    public string LogStreamName { get; }

    /// <summary>
    /// Gets additional runtime-specific data that is not represented by the strongly typed properties.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Properties { get; }
}

/// <summary>
/// Invocation context for completion-only event functions and source-specific event contexts.
/// </summary>
public class EventContext : FunctionContext
{
    protected EventContext(
        FunctionContextMetadata metadata,
        IReadOnlyDictionary<string, object?>? properties = null)
        : base(metadata, properties) { }
}

/// <summary>
/// Invocation context for request/response functions and source-specific request contexts.
/// </summary>
public class RequestContext : FunctionContext
{
    protected RequestContext(
        FunctionContextMetadata metadata,
        IReadOnlyDictionary<string, object?>? properties = null)
        : base(metadata, properties) { }
}

/// <summary>
/// Invocation context shared by record-oriented functions and source-specific contexts.
/// </summary>
public class RecordContext : FunctionContext
{
    protected RecordContext(
        FunctionContextMetadata metadata,
        IReadOnlyDictionary<string, object?>? properties = null)
        : base(metadata, properties) { }
}