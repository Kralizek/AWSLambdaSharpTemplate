using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.DependencyInjection;

namespace Kralizek.Lambda
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection UseSqsHandler<TNotification, THandler>(this IServiceCollection services) 
            where TNotification : class
            where THandler : class, INotificationHandler<TNotification>
        {
            services.AddTransient<IEventHandler<SQSEvent>, SqsEventHandler<TNotification>>();

            services.AddTransient<INotificationHandler<TNotification>, THandler>();

            return services;
        }
    }
}
