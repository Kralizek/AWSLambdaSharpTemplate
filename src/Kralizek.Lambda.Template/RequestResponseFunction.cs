using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kralizek.Lambda;

public abstract class RequestResponseFunction<TInput, TOutput> : Function
{
    public async Task<TOutput> FunctionHandlerAsync(TInput input, ILambdaContext context)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var handler = scope.ServiceProvider.GetService<IRequestResponseHandler<TInput, TOutput>>();

            if (handler == null)
            {
                Logger.LogCritical("No {Handler} could be found", $"IRequestResponseHandler<{typeof(TInput).Name}, {typeof(TOutput).Name}>");
                throw new InvalidOperationException($"No IRequestResponseHandler<{typeof(TInput).Name}, {typeof(TOutput).Name}> could be found.");
            }

            Logger.LogInformation("Invoking handler");

            return await handler.HandleAsync(input, context).ConfigureAwait(false);
        }
    }

    protected void RegisterHandler<THandler>(IServiceCollection services) where THandler : class, IRequestResponseHandler<TInput, TOutput>
    { 
        services.AddTransient<IRequestResponseHandler<TInput, TOutput>, THandler>();
    }
}

public interface IRequestResponseHandler<TInput, TOutput>
{
    Task<TOutput> HandleAsync(TInput input, ILambdaContext context);
}