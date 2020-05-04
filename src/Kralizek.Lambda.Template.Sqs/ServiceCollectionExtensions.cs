using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.DependencyInjection;

namespace Kralizek.Lambda
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection UseSqsHandler<TMessage, THandler>(this IServiceCollection services)
            where TMessage : class
            where THandler : class, IMessageHandler<TMessage>
        {
            services.AddTransient<IEventHandler<SQSEvent>, SqsEventHandler<TMessage>>();

            services.AddTransient<IMessageHandler<TMessage>, THandler>();

            return services;
        }

        public static IServiceCollection UseAsyncSqsHandler<TMessage, THandler>(this IServiceCollection services, int maxDegreeOfParallelism = 1)
            where TMessage : class
            where THandler : class, IMessageHandler<TMessage>
        {
            services.AddSingleton(new ForEachAsyncHandlingOption {MaxDegreeOfParallelism = maxDegreeOfParallelism});
            services.AddTransient<IEventHandler<SQSEvent>, SqsForEachAsyncEventHandler<TMessage>>();
            services.AddTransient<IMessageHandler<TMessage>, THandler>();
            
            return services;
        }
    }
}
