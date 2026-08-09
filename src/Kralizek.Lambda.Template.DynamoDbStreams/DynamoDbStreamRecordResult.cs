namespace Kralizek.Lambda;

/// <summary>
/// Represents the outcome of processing a DynamoDB Streams record.
/// </summary>
public abstract class DynamoDbStreamRecordResult : LambdaRecordResult
{
    private DynamoDbStreamRecordResult(object value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the singleton result used when a record has been processed successfully.
    /// </summary>
    public static DynamoDbStreamRecordResult Success { get; } = new SuccessResult();

    /// <summary>
    /// Creates a result used when a record must be reported as failed.
    /// </summary>
    /// <param name="reason">An optional application-provided reason for the failure.</param>
    public static DynamoDbStreamRecordResult Failed(string? reason = null) => new FailureResult(reason);

    /// <inheritdoc />
    public override object Value { get; }

    /// <summary>
    /// Represents the successful case value of a DynamoDB Streams record result.
    /// </summary>
    public sealed class SuccessCase
    {
        internal static SuccessCase Instance { get; } = new();

        private SuccessCase()
        {
        }
    }

    /// <summary>
    /// Represents the failed case value of a DynamoDB Streams record result.
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

    /// <summary>
    /// Represents a failed DynamoDB Streams record result.
    /// </summary>
    public sealed class FailureResult : DynamoDbStreamRecordResult
    {
        internal FailureResult(string? reason)
            : base(new FailureCase(reason))
        {
        }

        /// <summary>
        /// Gets the optional application-provided failure reason.
        /// </summary>
        public string? Reason => ((FailureCase)Value).Reason;
    }

    private sealed class SuccessResult : DynamoDbStreamRecordResult
    {
        private SuccessResult()
            : base(SuccessCase.Instance)
        {
        }
    }
}