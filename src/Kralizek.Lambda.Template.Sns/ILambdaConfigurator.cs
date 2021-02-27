using System;
using Amazon.Lambda.SNSEvents;
using Microsoft.Extensions.DependencyInjection;

namespace Kralizek.Lambda
{
    public interface ILambdaConfigurator<TNotification, THandler> : ILambdaConfigurator
        where TNotification : class
        where THandler : class, INotificationHandler<TNotification>
    {
    }

    public class SnsLambdaConfigurator<TNotification, THandler> : ILambdaConfigurator<TNotification, THandler>
        where TNotification : class
        where THandler : class, INotificationHandler<TNotification>
    {
        public SnsLambdaConfigurator(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public IServiceCollection Services { get; }
    }

    public static class LambdaConfiguratorExtensions
    {
        public static ILambdaConfigurator<TNotification, THandler> UseParallelExecution<TNotification, THandler>(this ILambdaConfigurator<TNotification, THandler> configurator, int? maxDegreeOfParallelism = null)
            where TNotification : class
            where THandler : class, INotificationHandler<TNotification>
        {
            configurator.Services.AddTransient<IEventHandler<SNSEvent>, ParallelSnsEventHandler<TNotification>>();

            if (maxDegreeOfParallelism.HasValue)
            {
                configurator.Services.Configure<ParallelSnsExecutionOptions>(option => option.MaxDegreeOfParallelism = maxDegreeOfParallelism.Value);
            }

            return configurator;
        }
    }
}
