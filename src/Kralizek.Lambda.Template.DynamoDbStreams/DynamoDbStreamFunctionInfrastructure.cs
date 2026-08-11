using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Kralizek.Lambda;

/// <summary>
/// Infrastructure base for DynamoDB Streams function specializations.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class DynamoDbStreamFunctionBase<TRecordHandler>
    : RecordFunction<
        DynamoDBEvent,
        DynamoDBEvent.DynamodbStreamRecord,
        DynamoDbStreamRecordResult,
        StreamsEventResponse,
        RecordContext,
        TRecordHandler>
    where TRecordHandler : class, IRecordHandler<DynamoDBEvent.DynamodbStreamRecord, DynamoDbStreamRecordResult, RecordContext>
{
    protected override RecordContext CreateRecordContext(DynamoDBEvent envelope, ILambdaContext lambdaContext) =>
        FunctionContextFactory.CreateRecordContext(lambdaContext);

    protected override IEnumerable<DynamoDBEvent.DynamodbStreamRecord> GetRecords(DynamoDBEvent envelope) =>
        envelope.Records ?? Enumerable.Empty<DynamoDBEvent.DynamodbStreamRecord>();

    protected override void EnrichRecordActivity(Activity activity, DynamoDBEvent.DynamodbStreamRecord record, RecordContext context)
    {
        activity.SetTag("kralizek.aws.dynamodb.stream.event_id", record.EventID);
        activity.SetTag("kralizek.aws.dynamodb.stream.event_name", record.EventName);
        activity.SetTag("kralizek.aws.dynamodb.stream.sequence_number", record.Dynamodb?.SequenceNumber);

        if (!string.IsNullOrWhiteSpace(record.EventSourceArn))
        {
            activity.SetTag("cloud.resource_id", record.EventSourceArn);
            activity.SetTag("kralizek.aws.dynamodb.stream.arn", record.EventSourceArn);
        }
    }

    protected override StreamsEventResponse CreateResponse(IReadOnlyCollection<RecordProcessingResult> results)
    {
        var failures = new List<StreamsEventResponse.BatchItemFailure>();

        foreach (var result in results.Where(result => result.Result.Value is DynamoDbStreamRecordResult.FailureCase))
        {
            var sequenceNumber = result.Record.Dynamodb?.SequenceNumber;

            if (string.IsNullOrWhiteSpace(sequenceNumber))
            {
                throw new InvalidOperationException(
                    "A failed DynamoDB Streams record does not contain a sequence number.");
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

    protected override ValueTask<DynamoDbStreamRecordResult> HandleRecordExceptionAsync(
        DynamoDBEvent.DynamodbStreamRecord record,
        Exception exception,
        RecordContext context,
        CancellationToken cancellationToken)
    {
        Logger.LogError(
            exception,
            "Failed to process DynamoDB Streams record {EventId} with sequence number {SequenceNumber}",
            record.EventID,
            record.Dynamodb?.SequenceNumber);

        return ValueTask.FromResult(DynamoDbStreamRecordResult.Failed(exception.Message));
    }
}

internal static class DynamoDbStreamServiceRegistration
{
    public static void AddHandler<THandler>(IServiceCollection services)
        where THandler : class, IDynamoDbStreamRecordHandler =>
        services.TryAddScoped<THandler>();
}
