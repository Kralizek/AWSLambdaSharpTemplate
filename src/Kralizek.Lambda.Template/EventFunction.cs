using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kralizek.Lambda
{
    public abstract class EventFunction<TInput> : Function
    {
        public async Task FunctionHandlerAsync(TInput input, ILambdaContext context)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var handler = scope.ServiceProvider.GetService<IEventHandler<TInput>>();

                if (handler == null)
                {
                    Logger.LogCritical($"No IEventHandler<{typeof(TInput).Name}> could be found.");
                    throw new InvalidOperationException($"No IEventHandler<{typeof(TInput).Name}> could be found.");
                }

                Logger.LogInformation("Invoking handler");
                await handler.HandleAsync(input, context).ConfigureAwait(false);
            }
        }

        protected void RegisterHandler<THandler>(IServiceCollection services, ISerializer serializer = null) where THandler : class, IEventHandler<TInput>
        {
            if (serializer != null)
                services.AddSingleton(sp => serializer);
            services.AddTransient<IEventHandler<TInput>, THandler>();
        }
    }

    public interface IEventHandler<TInput>
    {
        Task HandleAsync(TInput input, ILambdaContext context);
    }
}