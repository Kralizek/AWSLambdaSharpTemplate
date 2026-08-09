namespace Kralizek.Lambda;

/// <summary>
/// Represents the outcome of processing an SQS record.
/// </summary>
public abstract class SqsRecordResult : LambdaRecordResult
{
    private SqsRecordResult()
    {
    }

    /// <summary>
    /// Gets the singleton result used when a record has been processed successfully.
    /// </summary>
    public static SqsRecordResult Success { get; } = new SuccessResult();

    /// <summary>
    /// Creates a result used when a record must be reported as failed.
    /// </summary>
    /// <param name="reason">An optional application-provided reason for the failure.</param>
    public static SqsRecordResult Failed(string? reason = null) => new FailureResult(reason);

    /// <inheritdoc />
    public override object Value => this;

    /// <summary>
    /// Represents a failed SQS record result.
    /// </summary>
    public sealed class FailureResult : SqsRecordResult
    {
        internal FailureResult(string? reason)
        {
            Reason = reason;
        }

        /// <summary>
        /// Gets the optional application-provided failure reason.
        /// </summary>
        public string? Reason { get; }
    }

    private sealed class SuccessResult : SqsRecordResult
    {
    }
}