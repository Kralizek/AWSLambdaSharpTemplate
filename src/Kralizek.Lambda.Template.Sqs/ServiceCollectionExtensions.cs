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
    }
}
