using System;
using Amazon.Lambda.SNSEvents;
using Microsoft.Extensions.DependencyInjection;

namespace Kralizek.Lambda
{
    public static class ServiceCollectionExtensions
    {
        [Obsolete("Use UseNotificationHandler<TNotification, THandler>(c => c.UseParallelExecution(maxDegreeOfParallelism))")]
        public static IServiceCollection ConfigureSnsParallelExecution(this IServiceCollection services, int maxDegreeOfParallelism)
        {
            services.Configure<ParallelSnsExecutionOptions>(option => option.MaxDegreeOfParallelism = maxDegreeOfParallelism);

            return services;
        }

        [Obsolete("Use UseNotificationHandler<TNotification, THandler>(c => c.UseParallelExecution())")]
        public static IServiceCollection UseNotificationHandler<TNotification, THandler>(this IServiceCollection services, bool enableParallelExecution)
            where TNotification : class
            where THandler : class, INotificationHandler<TNotification>
        {
            if (enableParallelExecution)
            {
                services.UseNotificationHandler<TNotification, THandler>(c => c.UseParallelExecution());
            }
            else
            {
                services.UseNotificationHandler<TNotification, THandler>();
            }

            return services;
        }

        public static IServiceCollection UseNotificationHandler<TNotification, THandler>(this IServiceCollection services, Action<ILambdaConfigurator<TNotification, THandler>> configure = null)
            where TNotification : class
            where THandler : class, INotificationHandler<TNotification>
        {
            services.AddOptions();

            services.AddSingleton<ISerializer, SystemTextJsonSerializer>();

            services.AddTransient<IEventHandler<SNSEvent>, SnsEventHandler<TNotification>>();

            if (configure != null)
            {
                var configurator = new SnsLambdaConfigurator<TNotification, THandler>(services);

                configure(configurator);
            }

            services.AddTransient<INotificationHandler<TNotification>, THandler>();

            return services;
        }
    }
}