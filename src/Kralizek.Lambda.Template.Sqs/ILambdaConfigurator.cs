using System;
using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.DependencyInjection;

namespace Kralizek.Lambda
{
    public interface ILambdaConfigurator<TMessage, THandler> : ILambdaConfigurator
        where TMessage : class
        where THandler : class, IMessageHandler<TMessage>
    {
    }

    public class SqsLambdaConfigurator<TMessage, THandler> : ILambdaConfigurator<TMessage, THandler>
        where TMessage : class
        where THandler : class, IMessageHandler<TMessage>
    {
        public SqsLambdaConfigurator(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public IServiceCollection Services { get; }
    }

    public static class LambdaConfiguratorExtensions
    {
        public static ILambdaConfigurator<TMessage, THandler> UseParallelExecution<TMessage, THandler>(this ILambdaConfigurator<TMessage, THandler> configurator, int? maxDegreeOfParallelism = null)
            where TMessage : class
            where THandler : class, IMessageHandler<TMessage>
        {
            configurator.Services.AddTransient<IEventHandler<SQSEvent>, ParallelSqsEventHandler<TMessage>>();

            if (maxDegreeOfParallelism.HasValue)
            {
                configurator.Services.Configure<ParallelSqsExecutionOptions>(option => option.MaxDegreeOfParallelism = maxDegreeOfParallelism.Value);
            }

            return configurator;
        }
    }
}
