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
            ServiceCollection services = new ServiceCollection();
            services.AddLogging();

            ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();

            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

            ConfigureExecution(context, loggerFactory);

            var handler = serviceProvider.GetService<IEventHandler<TInput>>();

            if (handler == null)
            {
                throw new InvalidOperationException($"No IEventHandler<{typeof(TInput).Name}> could be found.");
            }

            return handler.HandleAsync(input);
        }
    }

    public interface IEventHandler<TInput>
    {
        Task HandleAsync(TInput input);
    }
}