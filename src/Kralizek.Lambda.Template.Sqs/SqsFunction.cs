using Microsoft.Extensions.DependencyInjection;

namespace Kralizek.Lambda;

/// <summary>
/// A function base class for Lambda functions triggered by SQS that process raw SQS records.
/// </summary>
/// <typeparam name="THandler">The concrete handler type that processes each SQS record.</typeparam>
public abstract class SqsFunction<THandler>
    : SqsFunctionBase<RawSqsRecordHandler<THandler>>
    where THandler : class, ISqsRecordHandler
{
    protected override void ConfigureFrameworkServices(IServiceCollection services) =>
        SqsServiceRegistration.AddRawHandler<THandler>(services);
}

/// <summary>
/// A function base class for Lambda functions triggered by SQS that decode message bodies into application contracts.
/// </summary>
/// <typeparam name="TMessage">The decoded message type.</typeparam>
/// <typeparam name="THandler">The concrete handler type that processes each message.</typeparam>
public abstract class SqsFunction<TMessage, THandler>
    : SqsFunctionBase<SqsRecordHandler<TMessage, THandler>>
    where THandler : class, ISqsMessageHandler<TMessage>
{
    protected override void ConfigureFrameworkServices(IServiceCollection services) =>
        SqsServiceRegistration.AddDecodedHandler<TMessage, THandler>(services);
}
