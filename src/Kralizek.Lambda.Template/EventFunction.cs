using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kralizek.Lambda
{
    public abstract class EventFunction<TInput> : Function
    {
        public Task FunctionHandlerAsync(TInput input, ILambdaContext context)
        {
            using (ServiceProvider.CreateScope())
            {
                var handler = ServiceProvider.GetService<IEventHandler<TInput>>();

                if (handler == null)
                {
                    Logger.LogCritical($"No IEventHandler<{typeof(TInput).Name}> could be found.");
                    throw new InvalidOperationException($"No IEventHandler<{typeof(TInput).Name}> could be found.");
                }

                Logger.LogInformation("Invoking handler");
                return handler.HandleAsync(input, context);
            }
        }

        protected void RegisterHandler<THandler>(IServiceCollection services) where THandler : class, IEventHandler<TInput>
        {
            services.AddTransient<IEventHandler<TInput>, THandler>();
        }
    }

    public interface IEventHandler<TInput>
    {
        Task HandleAsync(TInput input, ILambdaContext context);
    }
}