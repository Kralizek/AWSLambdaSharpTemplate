using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kralizek.Lambda;

public abstract class KinesisStreamFunction<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>
    : KinesisStreamFunctionBase<RawKinesisStreamRecordHandler<THandler>>
    where THandler : class, IKinesisStreamRecordHandler
{
    protected sealed override void ConfigureFrameworkServices(IServiceCollection services) =>
        services.TryAddScoped<THandler>();
}

public abstract class KinesisStreamFunction<TPayload, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>
    : KinesisStreamFunctionBase<KinesisStreamRecordHandler<TPayload, THandler>>
    where THandler : class, IKinesisStreamRecordHandler<TPayload>
{
    protected sealed override void ConfigureFrameworkServices(IServiceCollection services)
    {
        services.TryAddScoped<THandler>();
        ConfigurePayloadServices(services);
    }

    protected virtual void ConfigurePayloadServices(IServiceCollection services) =>
        services.TryAddSingleton<IBinaryPayloadDecoder<TPayload>, JsonBinaryPayloadDecoder<TPayload>>();
}
