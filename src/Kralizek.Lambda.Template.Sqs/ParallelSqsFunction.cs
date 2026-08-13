using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kralizek.Lambda;

public abstract class ParallelSqsFunction<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>
    : ParallelSqsFunctionBase<RawSqsRecordHandler<THandler>>
    where THandler : class, ISqsRecordHandler
{
    protected sealed override void ConfigureFrameworkServices(IServiceCollection services) =>
        services.TryAddScoped<THandler>();
}

public abstract class ParallelSqsFunction<TMessage, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>
    : ParallelSqsFunctionBase<SqsRecordHandler<TMessage, THandler>>
    where THandler : class, ISqsMessageHandler<TMessage>
{
    protected sealed override void ConfigureFrameworkServices(IServiceCollection services)
    {
        services.TryAddScoped<THandler>();
        ConfigurePayloadServices(services);
        services.TryAddSingleton<IStringPayloadDecoder<TMessage>>(CreateDefaultDecoder);
    }

    protected virtual void ConfigurePayloadServices(IServiceCollection services) { }

    private static IStringPayloadDecoder<TMessage> CreateDefaultDecoder(IServiceProvider services)
    {
        var typeInfo = services.GetService<JsonTypeInfo<TMessage>>();
        if (typeInfo is not null)
        {
            return new JsonStringPayloadDecoder<TMessage>(typeInfo);
        }

        if (JsonSerializer.IsReflectionEnabledByDefault)
        {
            return new JsonStringPayloadDecoder<TMessage>();
        }

        throw new InvalidOperationException($"No JsonTypeInfo<{typeof(TMessage).Name}> is registered.");
    }
}
