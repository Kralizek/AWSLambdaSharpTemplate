using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kralizek.Lambda;

public abstract class ParallelSnsFunction<THandler>
    : ParallelSnsFunctionBase<RawSnsRecordHandler<THandler>>
    where THandler : class, ISnsRecordHandler
{
    protected sealed override void ConfigureFrameworkServices(IServiceCollection services) =>
        services.TryAddScoped<THandler>();
}

public abstract class ParallelSnsFunction<TNotification, THandler>
    : ParallelSnsFunctionBase<SnsRecordHandler<TNotification, THandler>>
    where THandler : class, ISnsNotificationHandler<TNotification>
{
    protected sealed override void ConfigureFrameworkServices(IServiceCollection services)
    {
        services.TryAddScoped<THandler>();
        ConfigurePayloadServices(services);
    }

    protected virtual void ConfigurePayloadServices(IServiceCollection services) =>
        services.TryAddSingleton<IStringPayloadDecoder<TNotification>, JsonStringPayloadDecoder<TNotification>>();
}
