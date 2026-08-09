namespace Kralizek.Lambda;

/// <summary>
/// Represents successful processing of an SNS record.
/// </summary>
public sealed class SnsRecordResult : LambdaRecordResult
{
    private SnsRecordResult()
    {
    }

    /// <summary>
    /// Gets the singleton result used when an SNS record has completed processing.
    /// </summary>
    public static SnsRecordResult Completed { get; } = new();

    /// <inheritdoc />
    public override object Value => this;
}