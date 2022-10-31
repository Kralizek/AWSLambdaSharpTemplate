using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kralizek.Lambda;

/// <summary>
/// A base class used for all Request/Response Functions.
/// </summary>
/// <typeparam name="TInput">The type of the incoming request.</typeparam>
/// <typeparam name="TOutput">The type of the outgoing response.</typeparam>
public abstract class RequestResponseFunction<TInput, TOutput> : Function
{
    /// <summary>
    /// The entrypoint used by the Lambda runtime for executing the function. 
    /// </summary>
    /// <param name="input">The incoming request.</param>
    /// <param name="context">A representation of the execution context.</param>
    /// <exception cref="InvalidOperationException">The exception is thrown if no handler is registered for the incoming input.</exception>
    public async Task<TOutput> FunctionHandlerAsync(TInput input, ILambdaContext context)
    {
        using var scope = ServiceProvider.CreateScope();

        var handler = scope.ServiceProvider.GetService<IRequestResponseHandler<TInput, TOutput>>();

        if (handler == null)
        {
            Logger.LogCritical("No {Handler} could be found", $"IRequestResponseHandler<{typeof(TInput).Name}, {typeof(TOutput).Name}>");
            throw new InvalidOperationException($"No IRequestResponseHandler<{typeof(TInput).Name}, {typeof(TOutput).Name}> could be found.");
        }

        Logger.LogInformation("Invoking handler");

        return await handler.HandleAsync(input, context).ConfigureAwait(false);
    }

    /// <summary>
    /// Registers the handler for the request ot type <typeparamref name="TInput"/>.
    /// </summary>
    /// <param name="services">The collections of services.</param>
    /// <param name="lifetime">The lifetime of the handler. Defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <typeparam name="THandler">The type of the handler for requests of type <typeparamref name="TInput"/>.</typeparam>
    protected void RegisterHandler<THandler>(IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Transient) where THandler : class, IRequestResponseHandler<TInput, TOutput>
    { 
        services.Add(ServiceDescriptor.Describe(typeof(IRequestResponseHandler<TInput, TOutput>), typeof(THandler), lifetime));
    }
}

/// <summary>
/// An interface that describes an handler for events with inputs of type <typeparamref name="TInput"/>.
/// </summary>
/// <typeparam name="TInput">The type of the incoming request.</typeparam>
/// <typeparam name="TOutput">The type of the outgoing response.</typeparam>
public interface IRequestResponseHandler<in TInput, TOutput>
{
    /// <summary>
    /// The method used to handle the incoming event.
    /// </summary>
    /// <param name="input">The incoming request.</param>
    /// <param name="context">A representation of the execution context.</param>
    /// <returns>The produced output.</returns>
    Task<TOutput> HandleAsync(TInput? input, ILambdaContext context);
}