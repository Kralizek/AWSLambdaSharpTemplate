using Microsoft.Extensions.DependencyInjection;

namespace Kralizek.Lambda;

/// <summary>
/// An SNS function specialization that processes raw records with bounded parallelism.
/// </summary>
/// <typeparam name="THandler">The concrete handler type that processes each SNS record.</typeparam>
public abstract class ParallelSnsFunction<THandler>
    : ParallelSnsFunctionBase<RawSnsRecordHandler<THandler>>
    where THandler : class, ISnsRecordHandler
{
    protected override void ConfigureFrameworkServices(IServiceCollection services) =>
        SnsServiceRegistration.AddRawHandler<THandler>(services);
}

/// <summary>
/// An SNS function specialization that processes decoded notifications with bounded parallelism.
/// </summary>
/// <typeparam name="TNotification">The decoded notification type.</typeparam>
/// <typeparam name="THandler">The concrete handler type that processes each notification.</typeparam>
public abstract class ParallelSnsFunction<TNotification, THandler>
    : ParallelSnsFunctionBase<SnsRecordHandler<TNotification, THandler>>
    where THandler : class, ISnsNotificationHandler<TNotification>
{
    protected override void ConfigureFrameworkServices(IServiceCollection services) =>
        SnsServiceRegistration.AddDecodedHandler<TNotification, THandler>(services);
}
