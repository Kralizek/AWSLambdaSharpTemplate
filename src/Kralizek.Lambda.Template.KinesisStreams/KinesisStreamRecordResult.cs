namespace Kralizek.Lambda;

/// <summary>
/// Represents the outcome of processing a Kinesis Streams record.
/// </summary>
public sealed class KinesisStreamRecordResult : LambdaRecordResult
{
    private KinesisStreamRecordResult(SuccessCase value)
    {
        Value = value;
    }

    private KinesisStreamRecordResult(FailureCase value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the singleton result used when a record has been processed successfully.
    /// </summary>
    public static KinesisStreamRecordResult Success { get; } = new(SuccessCase.Instance);

    /// <summary>
    /// Creates a result used when a record must be reported as failed.
    /// </summary>
    /// <param name="reason">The optional application-provided failure reason.</param>
    public static KinesisStreamRecordResult Failed(string? reason = null) => new(new FailureCase(reason));

    /// <inheritdoc />
    public override object? Value { get; }

    /// <summary>
    /// Represents the successful case value of a Kinesis Streams record result.
    /// </summary>
    public sealed class SuccessCase
    {
        internal static SuccessCase Instance { get; } = new();

        private SuccessCase()
        {
        }
    }

    /// <summary>
    /// Represents the failed case value of a Kinesis Streams record result.
    /// </summary>
    public sealed class FailureCase
    {
        internal FailureCase(string? reason)
        {
            Reason = reason;
        }

        /// <summary>
        /// Gets the optional application-provided failure reason.
        /// </summary>
        public string? Reason { get; }
    }
}