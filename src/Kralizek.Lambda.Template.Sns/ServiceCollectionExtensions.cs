using System;
using Amazon.Lambda.SNSEvents;
using Microsoft.Extensions.DependencyInjection;

namespace Kralizek.Lambda
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureSnsParallelExecution(this IServiceCollection services, int maxDegreeOfParallelism)
        {
            services.Configure<ParallelSnsExecutionOptions>(option => option.MaxDegreeOfParallelism = maxDegreeOfParallelism);

            return services;
        }

        public static IServiceCollection UseNotificationHandler<TNotification, THandler>(this IServiceCollection services, bool enableParallelExecution = false)
            where TNotification : class
            where THandler : class, INotificationHandler<TNotification>
        {
            services.AddOptions();

            if (enableParallelExecution)
            {
                services.AddTransient<IEventHandler<SNSEvent>, ParallelSnsEventHandler<TNotification>>();
            }
            else
            {
                services.AddTransient<IEventHandler<SNSEvent>, SnsEventHandler<TNotification>>();
            }

            services.AddTransient<INotificationHandler<TNotification>, THandler>();

            return services;            
        }
    }
}