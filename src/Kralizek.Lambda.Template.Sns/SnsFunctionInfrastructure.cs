using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;

namespace Kralizek.Lambda;

/// <summary>
/// Infrastructure base for SNS function specializations.
/// </summary>
/// <typeparam name="TRecordHandler">The infrastructure record handler used by the specialization.</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class SnsFunctionBase<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TRecordHandler>
    : RecordFunction<
        SNSEvent,
        SNSEvent.SNSRecord,
        SnsRecordResult,
        object?,
        RecordContext,
        TRecordHandler>
    where TRecordHandler : class, IRecordHandler<SNSEvent.SNSRecord, SnsRecordResult, RecordContext>
{
    protected override RecordContext CreateRecordContext(SNSEvent envelope, ILambdaContext lambdaContext) =>
        FunctionContextFactory.CreateRecordContext(lambdaContext);

    protected override IEnumerable<SNSEvent.SNSRecord> GetRecords(SNSEvent envelope) =>
        envelope.Records ?? Enumerable.Empty<SNSEvent.SNSRecord>();

    protected override void EnrichRecordActivity(Activity activity, SNSEvent.SNSRecord record, RecordContext context)
    {
        activity.SetTag("messaging.system", "aws.sns");
        activity.SetTag("messaging.operation.name", "process");
        activity.SetTag("messaging.operation.type", "process");
        activity.SetTag("messaging.message.id", record.Sns?.MessageId);

        var topicArn = record.Sns?.TopicArn;
        if (!string.IsNullOrWhiteSpace(topicArn))
        {
            activity.SetTag("aws.sns.topic.arn", topicArn);
            activity.SetTag("messaging.destination.name", topicArn[(topicArn.LastIndexOf(':') + 1)..]);
        }

        if (!string.IsNullOrWhiteSpace(record.EventSubscriptionArn))
        {
            activity.SetTag("kralizek.aws.sns.subscription.arn", record.EventSubscriptionArn);
        }
    }

    protected override object? CreateResponse(IReadOnlyCollection<RecordProcessingResult> results) => null;
}

/// <summary>
/// Infrastructure base for SNS functions that process records with bounded parallelism.
/// </summary>
/// <typeparam name="TRecordHandler">The infrastructure record handler used by the specialization.</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class ParallelSnsFunctionBase<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TRecordHandler> : SnsFunctionBase<TRecordHandler>
    where TRecordHandler : class, IRecordHandler<SNSEvent.SNSRecord, SnsRecordResult, RecordContext>
{
    /// <summary>
    /// Gets the maximum number of SNS records processed concurrently.
    /// </summary>
    protected virtual int MaxDegreeOfParallelism => Math.Max(2, Environment.ProcessorCount);

    protected override Task<IReadOnlyCollection<RecordProcessingResult>> ProcessRecordsAsync(
        SNSEvent envelope,
        RecordContext context,
        IServiceProvider invocationServices,
        CancellationToken cancellationToken) =>
        ProcessRecordsParallelAsync(
            envelope,
            context,
            invocationServices,
            MaxDegreeOfParallelism,
            cancellationToken);
}
