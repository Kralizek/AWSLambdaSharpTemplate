using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using Amazon.Lambda.Core;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kralizek.Lambda;

/// <summary>
/// A minimal host for handlers that process a single event and produce no meaningful response.
/// </summary>
/// <typeparam name="TInput">The type of the incoming event.</typeparam>
/// <typeparam name="TContext">The context type passed to the handler.</typeparam>
/// <typeparam name="THandler">The concrete handler type that processes the event.</typeparam>
#pragma warning disable S2436 // The generic roles are intentional and make the event contract explicit.
public abstract class MinimalEventFunction<TInput, TContext, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler> : MinimalLambdaFunction
#pragma warning restore S2436
    where TContext : EventContext
    where THandler : class, IEventHandler<TInput, TContext>
{
    protected sealed override void RegisterFrameworkServices(IServiceCollection services)
    {
        base.RegisterFrameworkServices(services);
        services.TryAddScoped<THandler>();
        ConfigureFrameworkServices(services);
    }

    /// <summary>
    /// Registers replaceable services for this event-function specialization.
    /// </summary>
    protected virtual void ConfigureFrameworkServices(IServiceCollection services)
    {
    }

    protected abstract TContext CreateContext(TInput input, ILambdaContext context);

    public virtual async Task FunctionHandlerAsync(TInput input, ILambdaContext context)
    {
        using var cts = CreateCancellationTokenSource(context);
        cts.Token.ThrowIfCancellationRequested();

        var eventContext = CreateContext(input, context);

        await using var invocationScope = ServiceProvider.CreateAsyncScope();
        var handler = invocationScope.ServiceProvider.GetRequiredService<THandler>();

        await handler.HandleAsync(input, eventContext, cts.Token).ConfigureAwait(false);
    }
}

/// <summary>
/// A minimal event-function host using the standard <see cref="EventContext"/>.
/// </summary>
public abstract class MinimalEventFunction<TInput, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler> : MinimalEventFunction<TInput, EventContext, THandler>
    where THandler : class, IEventHandler<TInput, EventContext>
{
    protected override EventContext CreateContext(TInput input, ILambdaContext context) =>
        FunctionContextFactory.CreateEventContext(context);
}
