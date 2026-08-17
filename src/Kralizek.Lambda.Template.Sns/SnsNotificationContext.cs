using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Amazon.Lambda.SNSEvents;

namespace Kralizek.Lambda;

/// <summary>
/// Provides Lambda invocation metadata together with SNS-specific notification metadata.
/// </summary>
public sealed class SnsNotificationContext : RecordContext
{
    private static readonly IReadOnlyDictionary<string, SNSEvent.MessageAttribute> EmptyMessageAttributes =
        new ReadOnlyDictionary<string, SNSEvent.MessageAttribute>(new Dictionary<string, SNSEvent.MessageAttribute>());

    private SnsNotificationContext(
        RecordContext invocationContext,
        SNSEvent.SNSRecord record)
        : base(invocationContext, SnsNotificationContextExtensions.SnsRecordPropertyName, record)
    {
        var message = record.Sns ?? throw new InvalidOperationException("The SNS record does not contain an SNS message.");

        EventSource = record.EventSource;
        EventSubscriptionArn = record.EventSubscriptionArn;
        EventVersion = record.EventVersion;
        MessageId = message.MessageId;
        TopicArn = message.TopicArn;
        Subject = message.Subject;
        Timestamp = message.Timestamp;
        Type = message.Type;
        Signature = message.Signature;
        SignatureVersion = message.SignatureVersion;
        SigningCertUrl = message.SigningCertUrl;
        UnsubscribeUrl = message.UnsubscribeUrl;
        MessageAttributes = message.MessageAttributes is null
            ? EmptyMessageAttributes
            : new Dictionary<string, SNSEvent.MessageAttribute>(message.MessageAttributes);
    }

    /// <summary>
    /// Gets the SNS event source identifier.
    /// </summary>
    public string? EventSource { get; }

    /// <summary>
    /// Gets the ARN of the SNS subscription that invoked the function.
    /// </summary>
    public string? EventSubscriptionArn { get; }

    /// <summary>
    /// Gets the SNS event version.
    /// </summary>
    public string? EventVersion { get; }

    /// <summary>
    /// Gets the SNS message identifier.
    /// </summary>
    public string? MessageId { get; }

    /// <summary>
    /// Gets the ARN of the SNS topic.
    /// </summary>
    public string? TopicArn { get; }

    /// <summary>
    /// Gets the optional SNS message subject.
    /// </summary>
    public string? Subject { get; }

    /// <summary>
    /// Gets the SNS message timestamp.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets the SNS message type.
    /// </summary>
    public string? Type { get; }

    /// <summary>
    /// Gets the SNS message signature.
    /// </summary>
    public string? Signature { get; }

    /// <summary>
    /// Gets the SNS message signature version.
    /// </summary>
    public string? SignatureVersion { get; }

    /// <summary>
    /// Gets the SNS signing certificate URL.
    /// </summary>
    public string? SigningCertUrl { get; }

    /// <summary>
    /// Gets the SNS unsubscribe URL.
    /// </summary>
    public string? UnsubscribeUrl { get; }

    /// <summary>
    /// Gets the SNS message attributes.
    /// </summary>
    public IReadOnlyDictionary<string, SNSEvent.MessageAttribute> MessageAttributes { get; }

    internal static SnsNotificationContext Create(RecordContext invocationContext, SNSEvent.SNSRecord record)
    {
        ArgumentNullException.ThrowIfNull(invocationContext);
        ArgumentNullException.ThrowIfNull(record);

        return new SnsNotificationContext(invocationContext, record);
    }
}

/// <summary>
/// Provides access to AWS-specific values preserved by the SNS integration.
/// </summary>
public static class SnsNotificationContextExtensions
{
    internal const string SnsRecordPropertyName = "Kralizek.Lambda.Template.Sns.SnsRecord";

    /// <summary>
    /// Gets the original AWS SNS record preserved in the context property bag.
    /// </summary>
    public static SNSEvent.SNSRecord GetSnsRecord(this SnsNotificationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Properties.TryGetValue(SnsRecordPropertyName, out var value) && value is SNSEvent.SNSRecord record)
        {
            return record;
        }

        throw new InvalidOperationException("The SNS notification context does not contain an AWS SNS record.");
    }
}
