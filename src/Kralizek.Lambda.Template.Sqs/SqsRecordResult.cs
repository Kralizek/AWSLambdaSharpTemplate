namespace Kralizek.Lambda;

/// <summary>
/// Represents the outcome of processing an SQS record.
/// </summary>
public sealed class SqsRecordResult : LambdaRecordResult
{
    private SqsRecordResult()
    {
    }

    /// <summary>
    /// Gets the singleton result used when a record has been processed successfully.
    /// </summary>
    public static SqsRecordResult Completed { get; } = new();

    /// <summary>
    /// Gets the singleton result used when a record must be reported as failed.
    /// </summary>
    public static SqsRecordResult Failed { get; } = new();

    /// <inheritdoc />
    public override object Value => this;
}
