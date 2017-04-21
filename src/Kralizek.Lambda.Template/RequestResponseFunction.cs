using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kralizek.Lambda
{
    public abstract class RequestResponseFunction<TInput, TOutput> : Function
    {
        public Task<TOutput> FunctionHandlerAsync(TInput input, ILambdaContext context)
        {
            using (ServiceProvider.CreateScope())
            {
                var handler = ServiceProvider.GetService<IRequestResponseHandler<TInput, TOutput>>();

                if (handler == null)
                {
                    Logger.LogCritical($"No IRequestResponseHandler<{typeof(TInput).Name}, {typeof(TOutput).Name}> could be found.");
                    throw new InvalidOperationException($"No IRequestResponseHandler<{typeof(TInput).Name}, {typeof(TOutput).Name}> could be found.");
                }

                Logger.LogInformation("Invoking handler");

                return handler.HandleAsync(input, context);
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
}