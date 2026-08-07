using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Kralizek.Lambda;

/// <summary>
/// A function base class for Lambda functions triggered by SQS.
/// </summary>
/// <typeparam name="TMessage">The decoded message type.</typeparam>
/// <typeparam name="THandler">The concrete handler type that processes each message.</typeparam>
public abstract class SqsFunction<TMessage, THandler>
    : RecordFunction<
        SQSEvent,
        SQSEvent.SQSMessage,
        bool,
        SQSBatchResponse,
        RecordContext,
        SqsRecordHandler<TMessage, THandler>>
    where THandler : class, ISqsMessageHandler<TMessage>
{
    protected override void ConfigureFrameworkServices(IServiceCollection services)
    {
        base.ConfigureFrameworkServices(services);
        services.TryAddScoped<THandler>();
        services.TryAddSingleton<IStringPayloadDecoder<TMessage>, JsonStringPayloadDecoder<TMessage>>();
    }

    protected override RecordContext CreateRecordContext(SQSEvent envelope, ILambdaContext lambdaContext) =>
        FunctionContextFactory.CreateRecordContext(lambdaContext);

    protected override IEnumerable<SQSEvent.SQSMessage> GetRecords(SQSEvent envelope) => envelope.Records;

    protected override SQSBatchResponse CreateResponse(IReadOnlyCollection<RecordProcessingResult> results)
    {
        var failures = results
            .Where(result => !result.Result)
            .Select(result => new SQSBatchResponse.BatchItemFailure
            {
                ItemIdentifier = result.Record.MessageId
            })
            .ToList();

        return new SQSBatchResponse(failures);
    }

    protected override ValueTask<bool> HandleRecordExceptionAsync(
        SQSEvent.SQSMessage record,
        Exception exception,
        RecordContext context,
        CancellationToken cancellationToken)
    {
        Logger.LogError(exception, "Failed to process SQS record {MessageId}", record.MessageId);
        return ValueTask.FromResult(false);
    }
}