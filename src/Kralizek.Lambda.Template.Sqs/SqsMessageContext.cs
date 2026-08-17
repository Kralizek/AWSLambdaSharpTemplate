using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Amazon.Lambda.SQSEvents;

namespace Kralizek.Lambda;

/// <summary>
/// Provides Lambda invocation metadata together with SQS-specific message metadata.
/// </summary>
public sealed class SqsMessageContext : RecordContext
{
    private static readonly IReadOnlyDictionary<string, string> EmptyAttributes =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

    private static readonly IReadOnlyDictionary<string, SQSEvent.MessageAttribute> EmptyMessageAttributes =
        new ReadOnlyDictionary<string, SQSEvent.MessageAttribute>(new Dictionary<string, SQSEvent.MessageAttribute>());

    private SqsMessageContext(
        RecordContext invocationContext,
        SQSEvent.SQSMessage record)
        : base(invocationContext, SqsMessageContextExtensions.SqsMessagePropertyName, record)
    {
        MessageId = record.MessageId;
        ReceiptHandle = record.ReceiptHandle;
        Attributes = record.Attributes is null
            ? EmptyAttributes
            : new Dictionary<string, string>(record.Attributes);
        MessageAttributes = record.MessageAttributes is null
            ? EmptyMessageAttributes
            : new Dictionary<string, SQSEvent.MessageAttribute>(record.MessageAttributes);
        EventSource = record.EventSource;
        EventSourceArn = record.EventSourceArn;
        AwsRegion = record.AwsRegion;
    }

    /// <summary>
    /// Gets the SQS message identifier.
    /// </summary>
    public string? MessageId { get; }

    /// <summary>
    /// Gets the receipt handle associated with the SQS message.
    /// </summary>
    public string? ReceiptHandle { get; }

    /// <summary>
    /// Gets the SQS system attributes associated with the message.
    /// </summary>
    public IReadOnlyDictionary<string, string> Attributes { get; }

    /// <summary>
    /// Gets the application-defined SQS message attributes.
    /// </summary>
    public IReadOnlyDictionary<string, SQSEvent.MessageAttribute> MessageAttributes { get; }

    /// <summary>
    /// Gets the AWS event source identifier.
    /// </summary>
    public string? EventSource { get; }

    /// <summary>
    /// Gets the ARN of the SQS queue that produced the message.
    /// </summary>
    public string? EventSourceArn { get; }

    /// <summary>
    /// Gets the AWS region of the SQS event source.
    /// </summary>
    public string? AwsRegion { get; }

    internal static SqsMessageContext Create(RecordContext invocationContext, SQSEvent.SQSMessage record)
    {
        ArgumentNullException.ThrowIfNull(invocationContext);
        ArgumentNullException.ThrowIfNull(record);

        return new SqsMessageContext(invocationContext, record);
    }
}

/// <summary>
/// Provides access to AWS-specific values preserved by the SQS integration.
/// </summary>
public static class SqsMessageContextExtensions
{
    internal const string SqsMessagePropertyName = "Kralizek.Lambda.Template.Sqs.SqsMessage";

    /// <summary>
    /// Gets the original AWS SQS message preserved in this record context or one derived from it.
    /// </summary>
    public static SQSEvent.SQSMessage GetSqsMessage(this RecordContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Properties.TryGetValue(SqsMessagePropertyName, out var value) && value is SQSEvent.SQSMessage message)
        {
            return message;
        }

        throw new InvalidOperationException("The record context does not contain an AWS SQS message.");
    }
}
