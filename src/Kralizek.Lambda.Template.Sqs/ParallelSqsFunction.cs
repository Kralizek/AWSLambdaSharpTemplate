using Microsoft.Extensions.DependencyInjection;

namespace Kralizek.Lambda;

/// <summary>
/// An SQS function specialization that processes raw records with bounded parallelism.
/// </summary>
/// <typeparam name="THandler">The concrete handler type that processes each SQS record.</typeparam>
public abstract class ParallelSqsFunction<THandler>
    : ParallelSqsFunctionBase<RawSqsRecordHandler<THandler>>
    where THandler : class, ISqsRecordHandler
{
    protected override void ConfigureFrameworkServices(IServiceCollection services) =>
        SqsServiceRegistration.AddRawHandler<THandler>(services);
}

/// <summary>
/// An SQS function specialization that processes decoded messages with bounded parallelism.
/// </summary>
/// <typeparam name="TMessage">The decoded message type.</typeparam>
/// <typeparam name="THandler">The concrete handler type that processes each message.</typeparam>
public abstract class ParallelSqsFunction<TMessage, THandler>
    : ParallelSqsFunctionBase<SqsRecordHandler<TMessage, THandler>>
    where THandler : class, ISqsMessageHandler<TMessage>
{
    protected override void ConfigureFrameworkServices(IServiceCollection services) =>
        SqsServiceRegistration.AddDecodedHandler<TMessage, THandler>(services);
}
