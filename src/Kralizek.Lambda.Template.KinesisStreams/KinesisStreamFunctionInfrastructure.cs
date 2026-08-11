using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.KinesisEvents;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Kralizek.Lambda;

[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class KinesisStreamFunctionBase<TRecordHandler>
    : RecordFunction<
        KinesisEvent,
        KinesisEvent.KinesisEventRecord,
        KinesisStreamRecordResult,
        StreamsEventResponse,
        RecordContext,
        TRecordHandler>
    where TRecordHandler : class, IRecordHandler<KinesisEvent.KinesisEventRecord, KinesisStreamRecordResult, RecordContext>
{
    protected override RecordContext CreateRecordContext(KinesisEvent envelope, ILambdaContext lambdaContext) =>
        FunctionContextFactory.CreateRecordContext(lambdaContext);

    protected override IEnumerable<KinesisEvent.KinesisEventRecord> GetRecords(KinesisEvent envelope) =>
        envelope.Records ?? Enumerable.Empty<KinesisEvent.KinesisEventRecord>();

    protected override void EnrichRecordActivity(Activity activity, KinesisEvent.KinesisEventRecord record, RecordContext context)
    {
        activity.SetTag("messaging.system", "aws_kinesis");
        activity.SetTag("messaging.operation.name", "process");
        activity.SetTag("messaging.operation.type", "process");
        activity.SetTag("messaging.message.id", record.Kinesis?.SequenceNumber);
        activity.SetTag("kralizek.aws.kinesis.event_id", record.EventId);
        activity.SetTag("kralizek.aws.kinesis.sequence_number", record.Kinesis?.SequenceNumber);
        activity.SetTag("kralizek.aws.kinesis.partition_key", record.Kinesis?.PartitionKey);
    }

    protected override bool IsSuccessfulRecordResult(KinesisStreamRecordResult result) =>
        result.Value is KinesisStreamRecordResult.SuccessCase;

    protected override StreamsEventResponse CreateResponse(IReadOnlyCollection<RecordProcessingResult> results)
    {
        var failures = new List<StreamsEventResponse.BatchItemFailure>();

        foreach (var result in results.Where(result => result.Result.Value is KinesisStreamRecordResult.FailureCase))
        {
            var sequenceNumber = result.Record.Kinesis?.SequenceNumber;

            if (string.IsNullOrWhiteSpace(sequenceNumber))
            {
                throw new InvalidOperationException("A failed Kinesis Streams record does not contain a sequence number.");
            }

            failures.Add(new StreamsEventResponse.BatchItemFailure
            {
                ItemIdentifier = sequenceNumber
            });
        }

        return new StreamsEventResponse
        {
            BatchItemFailures = failures
        };
    }

    protected override ValueTask<KinesisStreamRecordResult> HandleRecordExceptionAsync(
        KinesisEvent.KinesisEventRecord record,
        Exception exception,
        RecordContext context,
        CancellationToken cancellationToken)
    {
        Logger.LogError(
            exception,
            "Failed to process Kinesis Streams record {EventId} with sequence number {SequenceNumber}",
            record.EventId,
            record.Kinesis?.SequenceNumber);

        return ValueTask.FromResult(KinesisStreamRecordResult.Failed(exception.Message));
    }
}

internal static class KinesisStreamServiceRegistration
{
    public static void AddRawHandler<THandler>(IServiceCollection services)
        where THandler : class, IKinesisStreamRecordHandler =>
        services.TryAddScoped<THandler>();

    public static void AddDecodedHandler<TPayload, THandler>(IServiceCollection services)
        where THandler : class, IKinesisStreamRecordHandler<TPayload>
    {
        services.TryAddScoped<THandler>();
        services.TryAddSingleton<IBinaryPayloadDecoder<TPayload>, JsonBinaryPayloadDecoder<TPayload>>();
    }
}
