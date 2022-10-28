using System;
using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kralizek.Lambda
{
    public static class ServiceCollectionExtensions
    {
        [Obsolete("Use `services.UseQueueMessageHandler<TMessage,THandler>().UseParallelExecution(maxDegreeOfParallelism);` instead.")]
        public static IServiceCollection ConfigureSnsParallelExecution(this IServiceCollection services, int maxDegreeOfParallelism)
        {
            services.Configure<ParallelSqsExecutionOptions>(option => option.MaxDegreeOfParallelism = maxDegreeOfParallelism);

            return services;
        }
        
        public static IMessageHandlerConfigurator<TMessage> WithParallelExecution<TMessage>(this IMessageHandlerConfigurator<TMessage> configurator, int? maxDegreeOfParallelism = null)
            where TMessage : class
        {
            ArgumentNullException.ThrowIfNull(configurator);

            if (maxDegreeOfParallelism <= 1) throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism), $"{nameof(maxDegreeOfParallelism)} must be greater than 1");

            configurator.Services.AddTransient<IEventHandler<SQSEvent>, ParallelSqsEventHandler<TMessage>>();

            if (maxDegreeOfParallelism.HasValue)
            {
                configurator.Services.Configure<ParallelSqsExecutionOptions>(options => options.MaxDegreeOfParallelism = maxDegreeOfParallelism.Value);
            }

            return configurator;
        }
        
        public static IServiceCollection UseCustomMessageSerializer<TSerializer>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TSerializer : IMessageSerializer
        {
            ArgumentNullException.ThrowIfNull(services);
            
            services.Add(ServiceDescriptor.Describe(typeof(IMessageSerializer), typeof(TSerializer), lifetime));

            return services;
        }

        [Obsolete("Use `services.UseQueueMessageHandler<TMessage, THandler>();` instead.")]
        public static IServiceCollection UseSqsHandler<TMessage, THandler>(this IServiceCollection services, bool enableParallelExecution = false)
            where TMessage : class
            where THandler : class, IMessageHandler<TMessage>
        {
            var configurator = UseQueueMessageHandler<TMessage, THandler>(services);

            if (enableParallelExecution)
            {
                configurator.WithParallelExecution();
            }

            return services;
        }

        public static IMessageHandlerConfigurator<TMessage> UseQueueMessageHandler<TMessage, THandler>(this IServiceCollection services)
            where TMessage : class
            where THandler : class, IMessageHandler<TMessage>
        {
            services.AddOptions();
            
            services.AddTransient<IEventHandler<SQSEvent>, SqsEventHandler<TMessage>>();
            
            services.TryAddSingleton<IMessageSerializer, DefaultJsonMessageSerializer>();

            services.AddTransient<IMessageHandler<TMessage>, THandler>();

            var configurator = new MessageHandlerConfigurator<TMessage>(services);

            return configurator;
        }
    }

    public interface IMessageHandlerConfigurator<TMessage>
        where TMessage : class
    {
        IServiceCollection Services { get; }
    }

    internal sealed class MessageHandlerConfigurator<TMessage> : IMessageHandlerConfigurator<TMessage>
        where TMessage : class
    {
        public MessageHandlerConfigurator(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }
        
        public IServiceCollection Services { get; }
    }
}
