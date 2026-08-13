using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kralizek.Lambda;

public abstract class KinesisStreamFunction<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>
    : KinesisStreamFunctionBase<RawKinesisStreamRecordHandler<THandler>>
    where THandler : class, IKinesisStreamRecordHandler
{
    protected sealed override void ConfigureFrameworkServices(IServiceCollection services) => services.TryAddScoped<THandler>();
}

public abstract class KinesisStreamFunction<TPayload, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>
    : KinesisStreamFunctionBase<KinesisStreamRecordHandler<TPayload, THandler>>
    where THandler : class, IKinesisStreamRecordHandler<TPayload>
{
    protected sealed override void ConfigureFrameworkServices(IServiceCollection services)
    {
        services.TryAddScoped<THandler>();
        ConfigurePayloadServices(services);
        services.TryAddSingleton<IBinaryPayloadDecoder<TPayload>>(CreateDefaultDecoder);
    }

    protected virtual void ConfigurePayloadServices(IServiceCollection services) { }

    private static IBinaryPayloadDecoder<TPayload> CreateDefaultDecoder(IServiceProvider services)
    {
        var typeInfo = services.GetService<JsonTypeInfo<TPayload>>();
        if (typeInfo is not null)
        {
            return new JsonBinaryPayloadDecoder<TPayload>(typeInfo);
        }

        if (JsonSerializer.IsReflectionEnabledByDefault)
        {
            return new JsonBinaryPayloadDecoder<TPayload>();
        }

        throw new InvalidOperationException($"No JsonTypeInfo<{typeof(TPayload).Name}> is registered.");
    }
}
