using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kralizek.Lambda;

/// <summary>
/// A base class used for all Event Functions.
/// </summary>
/// <typeparam name="TInput">The type of the incoming request.</typeparam>
public abstract class EventFunction<TInput> : Function
{
    /// <summary>
    /// The entrypoint used by the Lambda runtime for executing the function. 
    /// </summary>
    /// <param name="input">The incoming request.</param>
    /// <param name="context">A representation of the execution context.</param>
    /// <exception cref="InvalidOperationException">The exception is thrown if no handler is registered for the incoming input.</exception>
    public async Task FunctionHandlerAsync(TInput? input, ILambdaContext context)
    {
        using var scope = ServiceProvider.CreateScope();

        var handler = scope.ServiceProvider.GetService<IEventHandler<TInput>>();

        if (handler == null)
        {
            Logger.LogCritical("No {Handler} could be found", $"IEventHandler<{typeof(TInput).Name}>");
            throw new InvalidOperationException($"No IEventHandler<{typeof(TInput).Name}> could be found.");
        }

        Logger.LogInformation("Invoking handler");
        await handler.HandleAsync(input, context).ConfigureAwait(false);
    }

    /// <summary>
    /// Registers the handler for the request ot type <typeparamref name="TInput"/>.
    /// </summary>
    /// <param name="services">The collections of services.</param>
    /// <param name="lifetime">The lifetime of the handler. Defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <typeparam name="THandler">The type of the handler for requests of type <typeparamref name="TInput"/>.</typeparam>
    protected void RegisterHandler<THandler>(IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Transient) where THandler : class, IEventHandler<TInput>
    {
        services.Add(ServiceDescriptor.Describe(typeof(IEventHandler<TInput>), typeof(THandler), lifetime));
    }
}

/// <summary>
/// An interface that describes an handler for events with inputs of type <typeparamref name="TInput"/>.
/// </summary>
/// <typeparam name="TInput">The type of the incoming request.</typeparam>
public interface IEventHandler<in TInput>
{
    /// <summary>
    /// The method used to handle the incoming event.
    /// </summary>
    /// <param name="input">The incoming request.</param>
    /// <param name="context">A representation of the execution context.</param>
    Task HandleAsync(TInput? input, ILambdaContext context);
}