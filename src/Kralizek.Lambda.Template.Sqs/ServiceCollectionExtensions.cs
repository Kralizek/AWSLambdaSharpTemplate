using System;
using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kralizek.Lambda;

public static class ServiceCollectionExtensions
{
    [Obsolete("Use `services.UseQueueMessageHandler<TMessage,THandler>().UseParallelExecution(maxDegreeOfParallelism);` instead.")]
    public static IServiceCollection ConfigureSnsParallelExecution(this IServiceCollection services, int maxDegreeOfParallelism)
    {
        services.Configure<ParallelSqsExecutionOptions>(option => option.MaxDegreeOfParallelism = maxDegreeOfParallelism);

        return services;
    }
        
    /// <summary>
    /// Customizes the registration of the <see cref="IMessageHandler{TMessage}"/> to process the records in parallel.
    /// </summary>
    /// <param name="configurator">A configurator used to facilitate the configuration of the <see cref="IMessageHandler{TMessage}"/>.</param>
    /// <param name="maxDegreeOfParallelism">The top limit of concurrent threads processing the incoming notifications.</param>
    /// <typeparam name="TMessage">The internal type of the SQS messages.</typeparam>
    /// <returns>The <paramref name="configurator"/> once it has been configured.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="maxDegreeOfParallelism"/> is less or equal than <c>1</c>.</exception>    
    public static IMessageHandlerConfigurator<TMessage> WithParallelExecution<TMessage>(this IMessageHandlerConfigurator<TMessage> configurator, int? maxDegreeOfParallelism = null)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        if (maxDegreeOfParallelism <= 1) throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism), $"{nameof(maxDegreeOfParallelism)} must be greater than 1");

        configurator.Services.AddTransient<IEventHandler<SQSEvent>, ParallelSqsEventHandler<TMessage>>();
        configurator.Services.AddTransient<IRequestResponseHandler<SQSEvent, SQSBatchResponse>, ParallelSqsEventHandler<TMessage>>();

        if (maxDegreeOfParallelism.HasValue)
        {
            configurator.Services.Configure<ParallelSqsExecutionOptions>(options => options.MaxDegreeOfParallelism = maxDegreeOfParallelism.Value);
        }

        return configurator;
    }
        
    /// <summary>
    /// Registers a custom serializer for messages.
    /// </summary>
    /// <param name="services">The collection of service registrations.</param>
    /// <param name="lifetime">The lifetime used for the <see cref="IMessageSerializer"/> to register. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <typeparam name="TSerializer">The concrete type of the <see cref="IMessageSerializer"/> to be registered.</typeparam>
    /// <returns>The configured collection of service registrations.</returns>
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

    /// <summary>
    /// Registers all the services needed to handle messages of type <typeparamref name="TMessage"/>. 
    /// </summary>
    /// <param name="services">The collection of service registrations.</param>
    /// <param name="lifetime">The lifetime used for the <see cref="IMessageHandler{TMessage}"/> to register. Defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <typeparam name="TMessage">The internal type of the SNS notification.</typeparam>
    /// <typeparam name="THandler">The concrete type of the <see cref="IMessageHandler{TMessage}"/> to be registered.</typeparam>
    /// <returns>The configured collection of service registrations.</returns>
    public static IMessageHandlerConfigurator<TMessage> UseQueueMessageHandler<TMessage, THandler>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TMessage : class
        where THandler : class, IMessageHandler<TMessage>
    {
        services.AddOptions();
            
        services.AddTransient<IEventHandler<SQSEvent>, SqsEventHandler<TMessage>>();
        services.AddTransient<IRequestResponseHandler<SQSEvent, SQSBatchResponse>, SqsEventHandler<TMessage>>();

        services.TryAddSingleton<IMessageSerializer, DefaultJsonMessageSerializer>();

        services.Add(ServiceDescriptor.Describe(typeof(IMessageHandler<TMessage>), typeof(THandler), lifetime));

        var configurator = new MessageHandlerConfigurator<TMessage>(services);

        return configurator;
    }
}

/// <summary>
/// An interface used to represent a configurator of <see cref="IMessageHandler{TMessage}"/>.
/// </summary>
/// <typeparam name="TMessage">The internal type of the SQS message.</typeparam>
public interface IMessageHandlerConfigurator<TMessage>
    where TMessage : class
{
    /// <summary>
    /// The collection of service registrations.
    /// </summary>
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