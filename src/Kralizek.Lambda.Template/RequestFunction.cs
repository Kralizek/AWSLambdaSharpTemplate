using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Kralizek.Lambda;

/// <summary>
/// A function base class for handlers that process a single input and return a meaningful response.
/// </summary>
/// <typeparam name="TInput">The type of the incoming request.</typeparam>
/// <typeparam name="TOutput">The type of the response.</typeparam>
/// <typeparam name="THandler">The concrete handler type that processes the request.</typeparam>
public abstract class RequestFunction<TInput, TOutput, THandler> : LambdaFunction
    where THandler : class, IRequestHandler<TInput, TOutput>
{
    protected override void RegisterHandlers(IServiceCollection services)
    {
        services.TryAddTransient<THandler>();
    }

    /// <summary>
    /// The entry point called by the Lambda runtime.
    /// </summary>
    public async Task<TOutput> FunctionHandlerAsync(TInput input, ILambdaContext context)
    {
        using var scope = ServiceProvider.CreateScope();
        using var cts = CreateCancellationTokenSource(context);

        var handler = scope.ServiceProvider.GetRequiredService<THandler>();

        Logger.LogInformation("Invoking handler {Handler}", typeof(THandler).Name);

        return await handler.HandleAsync(input, cts.Token).ConfigureAwait(false);
    }
}

/// <summary>
/// The contract for handlers invoked by <see cref="RequestFunction{TInput,TOutput,THandler}"/>.
/// </summary>
/// <typeparam name="TInput">The type of the incoming request.</typeparam>
/// <typeparam name="TOutput">The type of the response.</typeparam>
public interface IRequestHandler<in TInput, TOutput>
{
    ValueTask<TOutput> HandleAsync(TInput input, CancellationToken cancellationToken);
}
