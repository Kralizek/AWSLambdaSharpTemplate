using Amazon.Lambda.SNSEvents;
using Microsoft.Extensions.DependencyInjection;

namespace Kralizek.Lambda
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection UseNotificationHandler<TNotification, THandler>(this IServiceCollection services) 
            where TNotification : class
            where THandler : class, INotificationHandler<TNotification>
        {
            services.AddTransient<IEventHandler<SNSEvent>, SnsEventHandler<TNotification>>();

            services.AddTransient<INotificationHandler<TNotification>, THandler>();

            return services;
        }

        public static IServiceCollection UseForEachAsyncSnsHandler<TNotification, THandler>(this IServiceCollection services, int maxDegreeOfParallelism = 1)
            where TNotification : class
            where THandler : class, INotificationHandler<TNotification>
        {
            services.AddSingleton(new ForEachAsyncHandlingOption { MaxDegreeOfParallelism = maxDegreeOfParallelism });
            services.AddTransient<IEventHandler<SNSEvent>, SnsForEachAsyncEventHandler<TNotification>>();

            services.AddTransient<INotificationHandler<TNotification>, THandler>();

            return services;
        }
    }
}