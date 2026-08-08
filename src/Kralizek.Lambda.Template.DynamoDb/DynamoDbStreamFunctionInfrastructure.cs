using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kralizek.Lambda;

/// <summary>
/// Infrastructure base for DynamoDB Streams function specializations.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class DynamoDbStreamFunctionBase<TRecordHandler>
    : RecordFunction<
        DynamoDBEvent,
        DynamoDBEvent.DynamodbStreamRecord,
        bool,
        StreamsEventResponse,
        RecordContext,
        TRecordHandler>
    where TRecordHandler : class, IRecordHandler<DynamoDBEvent.DynamodbStreamRecord, bool, RecordContext>
{
    protected override RecordContext CreateRecordContext(DynamoDBEvent envelope, ILambdaContext lambdaContext) =>
        FunctionContextFactory.CreateRecordContext(lambdaContext);

    protected override IEnumerable<DynamoDBEvent.DynamodbStreamRecord> GetRecords(DynamoDBEvent envelope) =>
        envelope.Records ?? Enumerable.Empty<DynamoDBEvent.DynamodbStreamRecord>();

    protected override StreamsEventResponse CreateResponse(IReadOnlyCollection<RecordProcessingResult> results)
    {
        var failures = new List<StreamsEventResponse.BatchItemFailure>();

        foreach (var result in results.Where(result => !result.Result))
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

    protected override ValueTask<bool> HandleRecordExceptionAsync(
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

        return ValueTask.FromResult(false);
    }
}

/// <summary>
/// Infrastructure base for DynamoDB Streams functions that process records with bounded parallelism.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class ParallelDynamoDbStreamFunctionBase<TRecordHandler>
    : DynamoDbStreamFunctionBase<TRecordHandler>
    where TRecordHandler : class, IRecordHandler<DynamoDBEvent.DynamodbStreamRecord, bool, RecordContext>
{
    /// <summary>
    /// Gets the maximum number of DynamoDB Streams records processed concurrently.
    /// </summary>
    protected virtual int MaxDegreeOfParallelism => Math.Max(2, Environment.ProcessorCount);

    protected override Task<IReadOnlyCollection<RecordProcessingResult>> ProcessRecordsAsync(
        DynamoDBEvent envelope,
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

internal static class DynamoDbStreamServiceRegistration
{
    public static void AddHandler<THandler>(IServiceCollection services)
        where THandler : class, IDynamoDbStreamRecordHandler =>
        services.TryAddScoped<THandler>();
}