using System;
using System.Collections.Generic;

using Amazon.Lambda.DynamoDBEvents;

namespace Kralizek.Lambda;

/// <summary>
/// Represents the DynamoDB item change carried by one stream record.
/// </summary>
public sealed class DynamoDbStreamItem
{
    private DynamoDbStreamItem(DynamoDBEvent.StreamRecord record)
    {
        ApproximateCreationDateTime = record.ApproximateCreationDateTime;
        Keys = Copy(record.Keys);
        NewImage = Copy(record.NewImage);
        OldImage = Copy(record.OldImage);
        SequenceNumber = record.SequenceNumber;
        SizeBytes = record.SizeBytes;
        StreamViewType = MapStreamViewType(record.StreamViewType);
    }

    public DateTime? ApproximateCreationDateTime { get; }

    public IReadOnlyDictionary<string, DynamoDBEvent.AttributeValue> Keys { get; }

    public IReadOnlyDictionary<string, DynamoDBEvent.AttributeValue> NewImage { get; }

    public IReadOnlyDictionary<string, DynamoDBEvent.AttributeValue> OldImage { get; }

    public string? SequenceNumber { get; }

    public long? SizeBytes { get; }

    public DynamoDbStreamViewType StreamViewType { get; }

    internal static DynamoDbStreamItem Create(DynamoDBEvent.DynamodbStreamRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new DynamoDbStreamItem(
            record.Dynamodb ?? throw new InvalidOperationException(
                "The DynamoDB Streams record does not contain DynamoDB stream data."));
    }

    private static DynamoDbStreamViewType MapStreamViewType(string? streamViewType) =>
        streamViewType switch
        {
            "KEYS_ONLY" => DynamoDbStreamViewType.KeysOnly,
            "NEW_IMAGE" => DynamoDbStreamViewType.NewImage,
            "OLD_IMAGE" => DynamoDbStreamViewType.OldImage,
            "NEW_AND_OLD_IMAGES" => DynamoDbStreamViewType.NewAndOldImages,
            _ => DynamoDbStreamViewType.Unknown
        };

    private static IReadOnlyDictionary<string, DynamoDBEvent.AttributeValue> Copy(
        IDictionary<string, DynamoDBEvent.AttributeValue>? source) =>
        source is null
            ? new Dictionary<string, DynamoDBEvent.AttributeValue>()
            : new Dictionary<string, DynamoDBEvent.AttributeValue>(source);
}