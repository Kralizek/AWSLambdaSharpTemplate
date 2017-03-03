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
            ConfigureExecution(context);

            using (CreateScope())
            {
                var handler = ServiceProvider.GetService<IRequestResponseHandler<TInput, TOutput>>();

                if (handler == null)
                {
                    throw new InvalidOperationException($"No IRequestResponseHandler<{typeof(TInput).Name}, {typeof(TOutput).Name}> could be found.");
                }

                Logger.LogInformation("Invoking handler");
                return handler.HandleAsync(input);
            }
        }
    }

    public interface IRequestResponseHandler<TInput, TOutput>
    {
        Task<TOutput> HandleAsync(TInput input);
    }
}