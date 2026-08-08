using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kralizek.Lambda;

/// <summary>
/// A function base class for Lambda functions triggered by SQS that process raw SQS records.
/// </summary>
/// <typeparam name="THandler">The concrete handler type that processes each SQS record.</typeparam>
public abstract class SqsFunction<THandler>
    : RecordFunction<
        SQSEvent,
        SQSEvent.SQSMessage,
        bool,
        SQSBatchResponse,
        RecordContext,
        RawSqsRecordHandler<THandler>>
    where THandler : class, ISqsRecordHandler
{
    protected override void ConfigureFrameworkServices(IServiceCollection services)
    {
        base.ConfigureFrameworkServices(services);
        services.TryAddScoped<THandler>();
    }

    protected override RecordContext CreateRecordContext(SQSEvent envelope, ILambdaContext lambdaContext) =>
        SqsFunctionInfrastructure.CreateRecordContext(lambdaContext);

    protected override IEnumerable<SQSEvent.SQSMessage> GetRecords(SQSEvent envelope) =>
        SqsFunctionInfrastructure.GetRecords(envelope);

    protected override SQSBatchResponse CreateResponse(IReadOnlyCollection<RecordProcessingResult> results) =>
        SqsFunctionInfrastructure.CreateResponse(
            results,
            static result => result.Record,
            static result => result.Result);

    protected override ValueTask<bool> HandleRecordExceptionAsync(
        SQSEvent.SQSMessage record,
        Exception exception,
        RecordContext context,
        CancellationToken cancellationToken) =>
        SqsFunctionInfrastructure.HandleRecordExceptionAsync(record, exception, Logger);
}

/// <summary>
/// A function base class for Lambda functions triggered by SQS that decode message bodies into application contracts.
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
        SqsFunctionInfrastructure.CreateRecordContext(lambdaContext);

    protected override IEnumerable<SQSEvent.SQSMessage> GetRecords(SQSEvent envelope) =>
        SqsFunctionInfrastructure.GetRecords(envelope);

    protected override SQSBatchResponse CreateResponse(IReadOnlyCollection<RecordProcessingResult> results) =>
        SqsFunctionInfrastructure.CreateResponse(
            results,
            static result => result.Record,
            static result => result.Result);

    protected override ValueTask<bool> HandleRecordExceptionAsync(
        SQSEvent.SQSMessage record,
        Exception exception,
        RecordContext context,
        CancellationToken cancellationToken) =>
        SqsFunctionInfrastructure.HandleRecordExceptionAsync(record, exception, Logger);
}