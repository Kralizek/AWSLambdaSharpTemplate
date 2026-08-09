using System;
using System.Collections.Generic;

using Amazon.Lambda.DynamoDBEvents;

namespace Kralizek.Lambda;

/// <summary>
/// Provides Lambda invocation metadata together with DynamoDB Streams record metadata.
/// </summary>
public sealed class DynamoDbStreamRecordContext : RecordContext
{
    private DynamoDbStreamRecordContext(
        FunctionContextMetadata metadata,
        IReadOnlyDictionary<string, object?> properties,
        DynamoDBEvent.DynamodbStreamRecord record)
        : base(metadata, properties)
    {
        EventId = record.EventID;
        EventName = record.EventName;
        EventSource = record.EventSource;
        EventSourceArn = record.EventSourceArn;
        AwsRegion = record.AwsRegion;
    }

    public string? EventId { get; }

    public string? EventName { get; }

    public string? EventSource { get; }

    public string? EventSourceArn { get; }

    public string? AwsRegion { get; }

    internal static DynamoDbStreamRecordContext Create(
        RecordContext invocationContext,
        DynamoDBEvent.DynamodbStreamRecord record)
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

        var properties = new Dictionary<string, object?>(invocationContext.Properties)
        {
            [DynamoDbStreamRecordContextExtensions.DynamoDbStreamRecordPropertyName] = record
        };

        return new DynamoDbStreamRecordContext(metadata, properties, record);
    }
}

/// <summary>
/// Provides access to AWS-specific values preserved by the DynamoDB Streams integration.
/// </summary>
public static class DynamoDbStreamRecordContextExtensions
{
    internal const string DynamoDbStreamRecordPropertyName =
        "Kralizek.Lambda.Template.DynamoDbStreams.DynamoDbStreamRecord";

    /// <summary>
    /// Gets the original AWS DynamoDB Streams record preserved in the context property bag.
    /// </summary>
    public static DynamoDBEvent.DynamodbStreamRecord GetDynamoDbStreamRecord(
        this DynamoDbStreamRecordContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Properties.TryGetValue(DynamoDbStreamRecordPropertyName, out var value)
            && value is DynamoDBEvent.DynamodbStreamRecord record)
        {
            return record;
        }

        throw new InvalidOperationException(
            "The DynamoDB stream record context does not contain an AWS DynamoDB Streams record.");
    }
}