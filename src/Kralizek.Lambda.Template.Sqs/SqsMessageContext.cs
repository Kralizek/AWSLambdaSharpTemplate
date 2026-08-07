using System;
using System.Collections.Generic;

using Amazon.Lambda.SQSEvents;

namespace Kralizek.Lambda;

/// <summary>
/// Provides invocation metadata together with the raw SQS record being processed.
/// </summary>
public sealed class SqsMessageContext : RecordContext
{
    private SqsMessageContext(
        FunctionContextMetadata metadata,
        IReadOnlyDictionary<string, object?> properties,
        SQSEvent.SQSMessage record)
        : base(metadata, properties)
    {
        Record = record;
    }

    /// <summary>
    /// Gets the raw SQS record, including attributes, message attributes, and receipt handle.
    /// </summary>
    public SQSEvent.SQSMessage Record { get; }

    internal static SqsMessageContext Create(RecordContext invocationContext, SQSEvent.SQSMessage record)
    {
        ArgumentNullException.ThrowIfNull(invocationContext);
        ArgumentNullException.ThrowIfNull(record);

        var metadata = new FunctionContextMetadata(
            invocationContext.AwsRequestId,
            invocationContext.FunctionName,
            invocationContext.FunctionVersion,
            invocationContext.InvokedFunctionArn,
            invocationContext.MemoryLimitInMB,
            invocationContext.RemainingTime,
            invocationContext.LogGroupName,
            invocationContext.LogStreamName);

        return new SqsMessageContext(metadata, invocationContext.Properties, record);
    }
}