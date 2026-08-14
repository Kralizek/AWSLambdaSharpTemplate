using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kralizek.Lambda;

public abstract class ParallelSqsFunction<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>
    : ParallelSqsFunctionBase<RawSqsRecordHandler<THandler>>
    where THandler : class, ISqsRecordHandler
{
    protected sealed override void RegisterFrameworkServices(IServiceCollection services)
    {
        base.RegisterFrameworkServices(services);
        services.TryAddScoped<THandler>();
    }
}

public abstract class ParallelSqsFunction<TMessage, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>
    : ParallelSqsFunctionBase<SqsRecordHandler<TMessage, THandler>>
    where THandler : class, ISqsMessageHandler<TMessage>
{
    protected sealed override void RegisterFrameworkServices(IServiceCollection services)
    {
        base.RegisterFrameworkServices(services);
        services.TryAddScoped<THandler>();
        services.TryAddSingleton<IStringPayloadDecoder<TMessage>>(SqsPayloadDecoderFactory.Create<TMessage>);
    }
}
