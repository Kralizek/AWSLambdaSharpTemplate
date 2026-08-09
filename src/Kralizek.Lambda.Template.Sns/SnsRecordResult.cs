namespace Kralizek.Lambda;

/// <summary>
/// Represents successful processing of an SNS record.
/// </summary>
public sealed class SnsRecordResult : LambdaRecordResult
{
    private SnsRecordResult(object value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the singleton result used when an SNS record has completed processing.
    /// </summary>
    public static SnsRecordResult Completed { get; } = new(CompletionCase.Instance);

    /// <inheritdoc />
    public override object Value { get; }

    /// <summary>
    /// Represents the completion case value of an SNS record result.
    /// </summary>
    public sealed class CompletionCase
    {
        internal static CompletionCase Instance { get; } = new();

        private CompletionCase()
        {
        }
    }
}