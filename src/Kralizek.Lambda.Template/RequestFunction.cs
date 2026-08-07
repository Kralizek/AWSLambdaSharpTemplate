using System.Threading;
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
/// <typeparam name="THandler">The concrete handler type that processes the request.</typeparam>
#pragma warning disable S2436 // The three generic roles are intentional and make the request contract explicit.
public abstract class RequestFunction<TInput, TOutput, THandler> : LambdaFunction
#pragma warning restore S2436
    where THandler : class, IRequestHandler<TInput, TOutput>
{
    private protected override void ConfigureFrameworkServices(IServiceCollection services)
    {
        base.ConfigureFrameworkServices(services);
        services.TryAddScoped<THandler>();
    }

    /// <summary>
    /// The entry point called by the Lambda runtime.
    /// </summary>
    public async Task<TOutput> FunctionHandlerAsync(TInput input, ILambdaContext context)
    {
        using var cts = CreateCancellationTokenSource(context);
        var requestContext = new RequestContext(context);

        return await InvokeAsync<THandler, TOutput>(
            cts.Token,
            (handler, cancellationToken) => handler.HandleAsync(input, requestContext, cancellationToken)).ConfigureAwait(false);
    }
}

/// <summary>
/// The contract for handlers invoked by <see cref="RequestFunction{TInput,TOutput,THandler}"/>.
/// </summary>
/// <typeparam name="TInput">The type of the incoming request.</typeparam>
/// <typeparam name="TOutput">The type of the response.</typeparam>
public interface IRequestHandler<in TInput, TOutput>
{
    ValueTask<TOutput> HandleAsync(TInput input, RequestContext context, CancellationToken cancellationToken);
}