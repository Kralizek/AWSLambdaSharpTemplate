using System;
using Microsoft.Extensions.DependencyInjection;

namespace Kralizek.Lambda
{
    public interface ILambdaConfigurator
    {
        IServiceCollection Services { get; }
    }

    public class LambdaConfigurator : ILambdaConfigurator
    {
        public LambdaConfigurator(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public IServiceCollection Services { get; }
    }

    public static class LambdaConfiguratorExtensions
    {
        public static ILambdaConfigurator UseCustomSerializer<TSerializer>(this ILambdaConfigurator configurator)
            where TSerializer : class, ISerializer
        {
            _ = configurator ?? throw new ArgumentNullException(nameof(configurator));

            configurator.Services.AddSingleton<ISerializer, TSerializer>();

            return configurator;
        }
    }
}
