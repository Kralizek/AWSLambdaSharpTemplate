using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.DependencyInjection;

namespace Kralizek.Lambda
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureSnsParallelExecution(this IServiceCollection services, int maxDegreeOfParallelism)
        {
            services.Configure<ParallelSqsExecutionOptions>(option => option.MaxDegreeOfParallelism = maxDegreeOfParallelism);

            return services;
        }

        public static IServiceCollection UseSqsHandler<TMessage, THandler>(this IServiceCollection services, bool enableParallelExecution = false, ISerializer serializer = null)
            where TMessage : class
            where THandler : class, IMessageHandler<TMessage>
        {
            services.AddOptions();

            if (serializer != null)
                services.AddSingleton(sp => serializer);

            if (enableParallelExecution)
            {
                services.AddTransient<IEventHandler<SQSEvent>, ParallelSqsEventHandler<TMessage>>();
            }
            else
            {
                services.AddTransient<IEventHandler<SQSEvent>, SqsEventHandler<TMessage>>();
            }

            services.AddTransient<IMessageHandler<TMessage>, THandler>();

            return services;
        }
    }
}
