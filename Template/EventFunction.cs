using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Template
{
    public abstract class EventFunction<TInput> : FunctionTemplate
    {
        public Task FunctionHandlerAsync(TInput input, ILambdaContext context)
        {
            ConfigureExecution(context);

            using (CreateScope())
            {
                var handler = ServiceProvider.GetService<IEventHandler<TInput>>();

                if (handler == null)
                {
                    throw new InvalidOperationException($"No IEventHandler<{typeof(TInput).Name}> could be found.");
                }

                Logger.LogInformation("Invoking handler");
                return handler.HandleAsync(input);
            }
        }

        protected void RegisterHandler<THandler>(IServiceCollection services) where THandler : class, IEventHandler<TInput>
        {
            services.AddTransient<IEventHandler<TInput>, THandler>();
        }
    }

    public interface IEventHandler<TInput>
    {
        Task HandleAsync(TInput input);
    }
}