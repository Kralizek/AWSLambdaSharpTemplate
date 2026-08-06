using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Kralizek.Lambda;

/// <summary>
/// A function base class for handlers that process a single event and produce no meaningful response.
/// </summary>
/// <typeparam name="TInput">The type of the incoming event.</typeparam>
/// <typeparam name="THandler">The concrete handler type that processes the event.</typeparam>
public abstract class EventFunction<TInput, THandler> : LambdaFunction
    where THandler : class, IEventHandler<TInput>
{
    protected override void RegisterHandlers(IServiceCollection services)
    {
        services.TryAddTransient<THandler>();
    }

    /// <summary>
    /// The entry point called by the Lambda runtime.
    /// </summary>
    public async Task FunctionHandlerAsync(TInput input, ILambdaContext context)
    {
        using var scope = ServiceProvider.CreateScope();
        using var cts = CreateCancellationTokenSource(context);

        var handler = scope.ServiceProvider.GetRequiredService<THandler>();

        Logger.LogInformation("Invoking handler {Handler}", typeof(THandler).Name);

        await handler.HandleAsync(input, cts.Token).ConfigureAwait(false);
    }
}

/// <summary>
/// The contract for handlers invoked by <see cref="EventFunction{TInput,THandler}"/>.
/// </summary>
/// <typeparam name="TInput">The type of the incoming event.</typeparam>
public interface IEventHandler<in TInput>
{
    ValueTask HandleAsync(TInput input, CancellationToken cancellationToken);
}