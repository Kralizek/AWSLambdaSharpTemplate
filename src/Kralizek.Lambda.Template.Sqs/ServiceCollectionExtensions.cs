using System;
using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.DependencyInjection;

namespace Kralizek.Lambda
{
    public static class ServiceCollectionExtensions
    {
        [Obsolete("Use UseSqsHandler<TMessage, THandler>(c => c.UseParallelExecution(maxDegreeOfParallelism))")]
        public static IServiceCollection ConfigureSqsParallelExecution(this IServiceCollection services, int maxDegreeOfParallelism)
        {
            services.Configure<ParallelSqsExecutionOptions>(option => option.MaxDegreeOfParallelism = maxDegreeOfParallelism);

            return services;
        }

        [Obsolete("Use UseSqsHandler<TMessage, THandler>(c => c.UseParallelExecution())")]
        public static IServiceCollection UseSqsHandler<TMessage, THandler>(this IServiceCollection services, bool enableParallelExecution)
            where TMessage : class
            where THandler : class, IMessageHandler<TMessage>
        {
            if (enableParallelExecution)
            {
                services.UseSqsHandler<TMessage, THandler>(c => c.UseParallelExecution());
            }
            else
            {
                services.UseSqsHandler<TMessage, THandler>();
            }

            return services;
        }

        public static IServiceCollection UseSqsHandler<TMessage, THandler>(this IServiceCollection services, Action<ILambdaConfigurator<TMessage, THandler>> configure = null)
            where TMessage : class
            where THandler : class, IMessageHandler<TMessage>
        {
            services.AddOptions();

            services.AddSingleton<ISerializer, SystemTextJsonSerializer>();

            services.AddTransient<IEventHandler<SQSEvent>, SqsEventHandler<TMessage>>();

            if (configure != null)
            {
                var configurator = new SqsLambdaConfigurator<TMessage, THandler>(services);

                configure(configurator);
            }

            services.AddTransient<IMessageHandler<TMessage>, THandler>();

            return services;
        }
    }
}
