namespace Kralizek.Lambda;

/// <summary>
/// Represents the outcome of processing a DynamoDB Streams record.
/// </summary>
public sealed class DynamoDbStreamRecordResult : LambdaRecordResult
{
    private DynamoDbStreamRecordResult()
    {
    }

    /// <summary>
    /// Gets the singleton result used when a record has been processed successfully.
    /// </summary>
    public static DynamoDbStreamRecordResult Completed { get; } = new();

    /// <summary>
    /// Gets the singleton result used when a record must be reported as failed.
    /// </summary>
    public static DynamoDbStreamRecordResult Failed { get; } = new();

    /// <inheritdoc />
    public override object Value => this;
}
