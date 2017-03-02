using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Template
{
    public abstract class RequestResponseFunction<TInput, TOutput> : FunctionTemplate
    {

        public Task<TOutput> FunctionHandlerAsync(TInput input, ILambdaContext context)
        {
            ServiceCollection services = new ServiceCollection();
            services.AddLogging();

            ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();

            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

            ConfigureExecution(context, loggerFactory);
            
            var handler = serviceProvider.GetService<IRequestResponseHandler<TInput, TOutput>>();

            if (handler == null)
            {
                throw new InvalidOperationException($"No IRequestResponseHandler<{typeof(TInput).Name}, {typeof(TOutput).Name}> could be found.");
            }

            return handler.HandleAsync(input);
        }

    }

    public interface IRequestResponseHandler<TInput, TOutput>
    {
        Task<TOutput> HandleAsync(TInput input);
    }
}