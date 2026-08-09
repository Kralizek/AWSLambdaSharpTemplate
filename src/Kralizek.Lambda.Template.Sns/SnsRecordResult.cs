namespace Kralizek.Lambda;

/// <summary>
/// Represents completion of processing an SNS record.
/// </summary>
public sealed class SnsRecordResult : LambdaRecordResult
{
    private SnsRecordResult()
    {
    }

    /// <summary>
    /// Gets the singleton result used when a record has been processed successfully.
    /// </summary>
    public static SnsRecordResult Completed { get; } = new();

    /// <inheritdoc />
    public override object Value => this;
}
