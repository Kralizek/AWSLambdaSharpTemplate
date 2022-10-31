using System;
using Amazon.Lambda.SNSEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kralizek.Lambda;

public static class ServiceCollectionExtensions
{
    [Obsolete("Use `services.UseNotificationHandler<TNotification,THandler>().UseParallelExecution(maxDegreeOfParallelism);` instead.")]
    public static IServiceCollection ConfigureSnsParallelExecution(this IServiceCollection services, int maxDegreeOfParallelism)
    {
        services.Configure<ParallelSnsExecutionOptions>(option => option.MaxDegreeOfParallelism = maxDegreeOfParallelism);

        return services;
    }

    /// <summary>
    /// Customizes the registration of the <see cref="INotificationHandler{TNotification}"/> to process the records in parallel.
    /// </summary>
    /// <param name="configurator">A configurator used to facilitate the configuration of the <see cref="INotificationHandler{TNotification}"/>.</param>
    /// <param name="maxDegreeOfParallelism">The top limit of concurrent threads processing the incoming notifications.</param>
    /// <typeparam name="TNotification">The internal type of the SNS notification.</typeparam>
    /// <returns>The <paramref name="configurator"/> once it has been configured.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="maxDegreeOfParallelism"/> is less or equal than <c>1</c>.</exception>
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

    /// <summary>
    /// Registers a custom serializer for notification messages.
    /// </summary>
    /// <param name="services">The collection of service registrations.</param>
    /// <param name="lifetime">The lifetime used for the <see cref="INotificationSerializer"/> to register. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <typeparam name="TSerializer">The concrete type of the <see cref="INotificationSerializer"/> to be registered.</typeparam>
    /// <returns>The configured collection of service registrations.</returns>
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

    /// <summary>
    /// Registers all the services needed to handle notifications of type <typeparamref name="TNotification"/>. 
    /// </summary>
    /// <param name="services">The collection of service registrations.</param>
    /// <param name="lifetime">The lifetime used for the <see cref="INotificationHandler{TNotification}"/> to register. Defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <typeparam name="TNotification">The internal type of the SNS notification.</typeparam>
    /// <typeparam name="THandler">The concrete type of the <see cref="INotificationHandler{TNotification}"/> to be registered.</typeparam>
    /// <returns>The configured collection of service registrations.</returns>
    public static INotificationHandlerConfigurator<TNotification> UseNotificationHandler<TNotification, THandler>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TNotification : class
        where THandler : class, INotificationHandler<TNotification>
    {
        services.AddOptions();
            
        services.AddTransient<IEventHandler<SNSEvent>, SnsEventHandler<TNotification>>();

        services.TryAddSingleton<INotificationSerializer, DefaultJsonNotificationSerializer>();

        services.Add(ServiceDescriptor.Describe(typeof(INotificationHandler<TNotification>), typeof(THandler), lifetime));

        var configurator = new NotificationHandlerConfigurator<TNotification>(services);

        return configurator;
    }
}

/// <summary>
/// An interface used to represent a configurator of <see cref="INotificationHandler{TNotification}"/>.
/// </summary>
/// <typeparam name="TNotification">The internal type of the SNS notification.</typeparam>
public interface INotificationHandlerConfigurator<TNotification>
    where TNotification : class
{
    /// <summary>
    /// The collection of service registrations.
    /// </summary>
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