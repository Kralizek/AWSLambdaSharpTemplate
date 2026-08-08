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
        SequenceNumber = record.Dynamodb?.SequenceNumber;
        StreamViewType = record.Dynamodb?.StreamViewType;
        ApproximateCreationDateTime = record.Dynamodb?.ApproximateCreationDateTime;
        SizeBytes = record.Dynamodb?.SizeBytes;
        Keys = Copy(record.Dynamodb?.Keys);
        NewImage = Copy(record.Dynamodb?.NewImage);
        OldImage = Copy(record.Dynamodb?.OldImage);
    }

    public string? EventId { get; }

    public string? EventName { get; }

    public string? EventSource { get; }

    public string? EventSourceArn { get; }

    public string? AwsRegion { get; }

    public string? SequenceNumber { get; }

    public string? StreamViewType { get; }

    public DateTime? ApproximateCreationDateTime { get; }

    public long? SizeBytes { get; }

    public IReadOnlyDictionary<string, DynamoDBEvent.AttributeValue> Keys { get; }

    public IReadOnlyDictionary<string, DynamoDBEvent.AttributeValue> NewImage { get; }

    public IReadOnlyDictionary<string, DynamoDBEvent.AttributeValue> OldImage { get; }

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

    private static IReadOnlyDictionary<string, DynamoDBEvent.AttributeValue> Copy(
        IDictionary<string, DynamoDBEvent.AttributeValue>? source) =>
        source is null
            ? new Dictionary<string, DynamoDBEvent.AttributeValue>()
            : new Dictionary<string, DynamoDBEvent.AttributeValue>(source);
}

/// <summary>
/// Provides access to AWS-specific values preserved by the DynamoDB Streams integration.
/// </summary>
public static class DynamoDbStreamRecordContextExtensions
{
    internal const string DynamoDbStreamRecordPropertyName =
        "Kralizek.Lambda.Template.DynamoDb.DynamoDbStreamRecord";

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