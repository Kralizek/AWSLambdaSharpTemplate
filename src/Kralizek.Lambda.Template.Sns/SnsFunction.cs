using Microsoft.Extensions.DependencyInjection;

namespace Kralizek.Lambda;

/// <summary>
/// A function base class for Lambda functions triggered by SNS that process raw SNS records.
/// </summary>
/// <typeparam name="THandler">The concrete handler type that processes each SNS record.</typeparam>
public abstract class SnsFunction<THandler>
    : SnsFunctionBase<RawSnsRecordHandler<THandler>>
    where THandler : class, ISnsRecordHandler
{
    protected override void ConfigureFrameworkServices(IServiceCollection services) =>
        SnsServiceRegistration.AddRawHandler<THandler>(services);
}

/// <summary>
/// A function base class for Lambda functions triggered by SNS that decode message payloads into application contracts.
/// </summary>
/// <typeparam name="TNotification">The decoded notification type.</typeparam>
/// <typeparam name="THandler">The concrete handler type that processes each notification.</typeparam>
public abstract class SnsFunction<TNotification, THandler>
    : SnsFunctionBase<SnsRecordHandler<TNotification, THandler>>
    where THandler : class, ISnsNotificationHandler<TNotification>
{
    protected override void ConfigureFrameworkServices(IServiceCollection services) =>
        SnsServiceRegistration.AddDecodedHandler<TNotification, THandler>(services);
}
