namespace Kralizek.Lambda;

/// <summary>
/// Identifies which DynamoDB item images are included in a stream record.
/// </summary>
public enum DynamoDbStreamViewType
{
    /// <summary>
    /// The stream view type is missing or not recognized by this version of the library.
    /// </summary>
    Unknown,

    /// <summary>
    /// Only the item keys are included.
    /// </summary>
    KeysOnly,

    /// <summary>
    /// The new item image is included.
    /// </summary>
    NewImage,

    /// <summary>
    /// The old item image is included.
    /// </summary>
    OldImage,

    /// <summary>
    /// Both the new and old item images are included.
    /// </summary>
    NewAndOldImages
}
