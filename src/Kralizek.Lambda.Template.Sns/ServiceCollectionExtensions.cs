using System;
using Amazon.Lambda.SNSEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kralizek.Lambda
{
    public static class ServiceCollectionExtensions
    {
        [Obsolete("Use `services.UseNotificationHandler<TNotification,THandler>().UseParallelExecution(maxDegreeOfParallelism);` instead.")]
        public static IServiceCollection ConfigureSnsParallelExecution(this IServiceCollection services, int maxDegreeOfParallelism)
        {
            services.Configure<ParallelSnsExecutionOptions>(option => option.MaxDegreeOfParallelism = maxDegreeOfParallelism);

            return services;
        }

        public static INotificationHandlerConfigurator<TNotification> WithParallelExecution<TNotification>(this INotificationHandlerConfigurator<TNotification> configurator, int? maxDegreeOfParallelism = null)
            where TNotification : class
        {
            ArgumentNullException.ThrowIfNull(configurator);

            if (maxDegreeOfParallelism <= 1) throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism), $"{nameof(maxDegreeOfParallelism)} must be greater than 1");

            configurator.Services.AddTransient<IEventHandler<SNSEvent>, ParallelSnsEventHandler<TNotification>>();

            if (maxDegreeOfParallelism.HasValue)
            {
                configurator.Services.Configure<ParallelSnsExecutionOptions>(options => options.MaxDegreeOfParallelism = maxDegreeOfParallelism.Value);
            }

            return configurator;
        }

        public static IServiceCollection UseCustomNotificationSerializer<TSerializer>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TSerializer : INotificationSerializer
        {
            ArgumentNullException.ThrowIfNull(services);
            
            services.Add(ServiceDescriptor.Describe(typeof(INotificationSerializer), typeof(TSerializer), lifetime));

            return services;
        }

        [Obsolete("Use `services.UseNotificationHandler<TNotification,THandler>().UseParallelExecution();` instead.")]
        public static IServiceCollection UseNotificationHandler<TNotification, THandler>(this IServiceCollection services, bool enableParallelExecution)
            where TNotification : class
            where THandler : class, INotificationHandler<TNotification>
        {
            var configurator = UseNotificationHandler<TNotification, THandler>(services);

            if (enableParallelExecution)
            {
                configurator.WithParallelExecution();
            }

            return services;
        }
        
        public static INotificationHandlerConfigurator<TNotification> UseNotificationHandler<TNotification, THandler>(this IServiceCollection services)
            where TNotification : class
            where THandler : class, INotificationHandler<TNotification>
        {
            services.AddOptions();
            
            services.AddTransient<IEventHandler<SNSEvent>, SnsEventHandler<TNotification>>();

            services.TryAddSingleton<INotificationSerializer, DefaultJsonNotificationSerializer>();

            services.AddTransient<INotificationHandler<TNotification>, THandler>();

            var configurator = new NotificationHandlerConfigurator<TNotification>(services);

            return configurator;
        }
    }
    
    public interface INotificationHandlerConfigurator<TNotification>
        where TNotification : class
    {
        IServiceCollection Services { get; }
    }

    internal sealed class NotificationHandlerConfigurator<TNotification> : INotificationHandlerConfigurator<TNotification>
        where TNotification : class
    {
        public NotificationHandlerConfigurator(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }
        
        public IServiceCollection Services { get; }
    }
}