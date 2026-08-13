using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kralizek.Lambda;

public abstract class SnsFunction<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>
    : SnsFunctionBase<RawSnsRecordHandler<THandler>>
    where THandler : class, ISnsRecordHandler
{
    protected sealed override void ConfigureFrameworkServices(IServiceCollection services) =>
        services.TryAddScoped<THandler>();
}

public abstract class SnsFunction<TNotification, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>
    : SnsFunctionBase<SnsRecordHandler<TNotification, THandler>>
    where THandler : class, ISnsNotificationHandler<TNotification>
{
    protected sealed override void ConfigureFrameworkServices(IServiceCollection services)
    {
        services.TryAddScoped<THandler>();
        ConfigurePayloadServices(services);
        services.TryAddSingleton<IStringPayloadDecoder<TNotification>>(SnsPayloadDecoderFactory.Create<TNotification>);
    }

    protected virtual void ConfigurePayloadServices(IServiceCollection services) { }
}

internal static class SnsPayloadDecoderFactory
{
    public static IStringPayloadDecoder<TNotification> Create<TNotification>(IServiceProvider services)
    {
        var typeInfo = services.GetService<JsonTypeInfo<TNotification>>();
        if (typeInfo is not null)
        {
            return new JsonStringPayloadDecoder<TNotification>(typeInfo);
        }

        if (JsonSerializer.IsReflectionEnabledByDefault)
        {
            return new JsonStringPayloadDecoder<TNotification>();
        }

        throw new InvalidOperationException($"No JsonTypeInfo<{typeof(TNotification).Name}> is registered.");
    }
}
