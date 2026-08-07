using System.Threading.Tasks;

using Amazon.Lambda.Core;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kralizek.Lambda;

/// <summary>
/// A function base class for handlers that process a single input and return a meaningful response.
/// </summary>
/// <typeparam name="TInput">The type of the incoming request.</typeparam>
/// <typeparam name="TOutput">The type of the response.</typeparam>
/// <typeparam name="TContext">The context type passed to the handler.</typeparam>
/// <typeparam name="THandler">The concrete handler type that processes the request.</typeparam>
#pragma warning disable S2436 // The generic roles are intentional and make the request contract explicit.
public abstract class RequestFunction<TInput, TOutput, TContext, THandler> : LambdaFunction
#pragma warning restore S2436
    where TContext : RequestContext
    where THandler : class, IRequestHandler<TInput, TOutput, TContext>
{
    protected override void ConfigureFrameworkServices(IServiceCollection services)
    {
        base.ConfigureFrameworkServices(services);
        services.TryAddScoped<THandler>();
    }

    /// <summary>
    /// Creates the strongly typed context passed to the request handler.
    /// </summary>
    protected abstract TContext CreateContext(TInput input, ILambdaContext context);

    /// <summary>
    /// The entry point called by the Lambda runtime.
    /// </summary>
    public async Task<TOutput> FunctionHandlerAsync(TInput input, ILambdaContext context)
    {
        using var cts = CreateCancellationTokenSource(context);
        var requestContext = CreateContext(input, context);

        await using var invocationScope = ServiceProvider.CreateAsyncScope();

        return await ExecuteHandlerAsync<THandler, TOutput>(
            invocationScope.ServiceProvider,
            cts.Token,
            (handler, cancellationToken) => handler.HandleAsync(input, requestContext, cancellationToken)).ConfigureAwait(false);
    }
}

/// <summary>
/// A function base class for handlers that use the standard <see cref="RequestContext"/>.
/// </summary>
#pragma warning disable S2436 // The three public roles intentionally hide the standard context type.
public abstract class RequestFunction<TInput, TOutput, THandler> : RequestFunction<TInput, TOutput, RequestContext, THandler>
#pragma warning restore S2436
    where THandler : class, IRequestHandler<TInput, TOutput, RequestContext>
{
    protected override RequestContext CreateContext(TInput input, ILambdaContext context) =>
        FunctionContextFactory.CreateRequestContext(context);
}