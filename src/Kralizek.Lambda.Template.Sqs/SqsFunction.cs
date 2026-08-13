using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kralizek.Lambda;

public abstract class SqsFunction<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>
    : SqsFunctionBase<RawSqsRecordHandler<THandler>>
    where THandler : class, ISqsRecordHandler
{
    protected sealed override void ConfigureFrameworkServices(IServiceCollection services) =>
        services.TryAddScoped<THandler>();
}

public abstract class SqsFunction<TMessage, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>
    : SqsFunctionBase<SqsRecordHandler<TMessage, THandler>>
    where THandler : class, ISqsMessageHandler<TMessage>
{
    protected sealed override void ConfigureFrameworkServices(IServiceCollection services)
    {
        services.TryAddScoped<THandler>();
        ConfigurePayloadServices(services);
    }

    /// <summary>
    /// Registers the payload decoder used by the typed SQS specialization.
    /// </summary>
    protected virtual void ConfigurePayloadServices(IServiceCollection services) =>
        services.TryAddSingleton<IStringPayloadDecoder<TMessage>, JsonStringPayloadDecoder<TMessage>>();
}
