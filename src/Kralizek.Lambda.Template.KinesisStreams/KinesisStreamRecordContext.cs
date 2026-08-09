using System;
using System.Collections.Generic;

using Amazon.Lambda.KinesisEvents;

namespace Kralizek.Lambda;

/// <summary>
/// Provides Lambda invocation metadata together with Kinesis Streams record metadata.
/// </summary>
public sealed class KinesisStreamRecordContext : RecordContext
{
    private KinesisStreamRecordContext(
        FunctionContextMetadata metadata,
        IReadOnlyDictionary<string, object?> properties,
        KinesisEvent.KinesisEventRecord record)
        : base(metadata, properties)
    {
        SequenceNumber = record.Kinesis?.SequenceNumber;
        PartitionKey = record.Kinesis?.PartitionKey;
        ApproximateArrivalTimestamp = record.Kinesis?.ApproximateArrivalTimestamp;
    }

    public string? SequenceNumber { get; }

    public string? PartitionKey { get; }

    public DateTime? ApproximateArrivalTimestamp { get; }

    internal static KinesisStreamRecordContext Create(RecordContext invocationContext, KinesisEvent.KinesisEventRecord record)
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
            [KinesisStreamRecordContextExtensions.KinesisRecordPropertyName] = record
        };

        return new KinesisStreamRecordContext(metadata, properties, record);
    }
}

/// <summary>
/// Provides access to the raw AWS Kinesis record associated with a <see cref="KinesisStreamRecordContext"/>.
/// </summary>
public static class KinesisStreamRecordContextExtensions
{
    internal const string KinesisRecordPropertyName = "Kralizek.Lambda.Template.KinesisStreams.KinesisRecord";

    /// <summary>
    /// Gets the raw AWS Kinesis record associated with the supplied record context.
    /// </summary>
    /// <param name="context">The Kinesis Streams record context.</param>
    /// <returns>The raw AWS Kinesis record.</returns>
    /// <exception cref="InvalidOperationException">The context does not contain an AWS Kinesis record.</exception>
    public static KinesisEvent.KinesisEventRecord GetKinesisRecord(this KinesisStreamRecordContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Properties.TryGetValue(KinesisRecordPropertyName, out var value) && value is KinesisEvent.KinesisEventRecord record)
        {
            return record;
        }

        throw new InvalidOperationException("The Kinesis Streams context does not contain an AWS Kinesis record.");
    }
}