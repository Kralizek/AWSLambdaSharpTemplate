using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;

using Microsoft.Extensions.Logging;

namespace Kralizek.Lambda;

internal static class SqsFunctionInfrastructure
{
    public static RecordContext CreateRecordContext(ILambdaContext lambdaContext) =>
        FunctionContextFactory.CreateRecordContext(lambdaContext);

    public static IEnumerable<SQSEvent.SQSMessage> GetRecords(SQSEvent envelope) => envelope.Records;

    public static SQSBatchResponse CreateResponse<TRecordProcessingResult>(
        IReadOnlyCollection<TRecordProcessingResult> results,
        Func<TRecordProcessingResult, SQSEvent.SQSMessage> getRecord,
        Func<TRecordProcessingResult, bool> getResult)
    {
        var failures = results
            .Where(result => !getResult(result))
            .Select(result => new SQSBatchResponse.BatchItemFailure
            {
                ItemIdentifier = getRecord(result).MessageId
            })
            .ToList();

        return new SQSBatchResponse(failures);
    }

    public static ValueTask<bool> HandleRecordExceptionAsync(
        SQSEvent.SQSMessage record,
        Exception exception,
        ILogger logger)
    {
        logger.LogError(exception, "Failed to process SQS record {MessageId}", record.MessageId);
        return ValueTask.FromResult(false);
    }

    public static int DefaultMaxDegreeOfParallelism => Math.Max(2, Environment.ProcessorCount);
}
