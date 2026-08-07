using System.Threading.Tasks;

using Amazon.Lambda.Core;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kralizek.Lambda;

/// <summary>
/// A function base class for handlers that process a single event and produce no meaningful response.
/// </summary>
/// <typeparam name="TInput">The type of the incoming event.</typeparam>
/// <typeparam name="TContext">The context type passed to the handler.</typeparam>
/// <typeparam name="THandler">The concrete handler type that processes the event.</typeparam>
#pragma warning disable S2436 // The generic roles are intentional and make the event contract explicit.
public abstract class EventFunction<TInput, TContext, THandler> : LambdaFunction
#pragma warning restore S2436
    where TContext : EventContext
    where THandler : class, IEventHandler<TInput, TContext>
{
    protected override void ConfigureFrameworkServices(IServiceCollection services)
    {
        base.ConfigureFrameworkServices(services);
        services.TryAddScoped<THandler>();
    }

    /// <summary>
    /// Creates the strongly typed context passed to the event handler.
    /// </summary>
    protected abstract TContext CreateContext(TInput input, ILambdaContext context);

    /// <summary>
    /// The entry point called by the Lambda runtime.
    /// </summary>
    public async Task FunctionHandlerAsync(TInput input, ILambdaContext context)
    {
        using var cts = CreateCancellationTokenSource(context);
        var eventContext = CreateContext(input, context);

        await using var invocationScope = ServiceProvider.CreateAsyncScope();

        await ExecuteHandlerAsync<THandler>(
            invocationScope.ServiceProvider,
            cts.Token,
            (handler, cancellationToken) => handler.HandleAsync(input, eventContext, cancellationToken)).ConfigureAwait(false);
    }
}

/// <summary>
/// A function base class for handlers that use the standard <see cref="EventContext"/>.
/// </summary>
public abstract class EventFunction<TInput, THandler> : EventFunction<TInput, EventContext, THandler>
    where THandler : class, IEventHandler<TInput, EventContext>
{
    protected override EventContext CreateContext(TInput input, ILambdaContext context) =>
        FunctionContextFactory.CreateEventContext(context);
}