using System;
using System.Threading;

using Amazon.Lambda.Core;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kralizek.Lambda;

internal sealed class LambdaHost
{
    public LambdaHost(IConfigurationRoot configuration, IServiceProvider serviceProvider)
    {
        Configuration = configuration;
        ServiceProvider = serviceProvider;
    }

    public IConfigurationRoot Configuration { get; }

    public IServiceProvider ServiceProvider { get; }
}

internal static class LambdaHostBuilder
{
    public static LambdaHost Build(
        Action<IConfigurationBuilder> configureConfiguration,
        Action<ILoggingBuilder> configureLogging,
        Action<IServiceCollection> registerFrameworkServices,
        Action<IServiceCollection, IConfiguration> configureServices)
    {
        var configurationBuilder = new ConfigurationBuilder();
        configureConfiguration(configurationBuilder);

        var configuration = configurationBuilder.Build();
        var executionEnvironment = new LambdaExecutionEnvironment
        {
            EnvironmentName = configuration["Environment"] ?? LambdaExecutionEnvironment.DevelopmentEnvironmentName,
            IsLambda = Environment.GetEnvironmentVariable("LAMBDA_RUNTIME_DIR") != null
        };

        var services = new ServiceCollection();
        services.AddSingleton<IExecutionEnvironment>(executionEnvironment);
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(configuration);
        services.AddLogging(logging =>
        {
            logging.AddLambdaLogger(new LambdaLoggerOptions
            {
                IncludeCategory = true,
                IncludeLogLevel = true,
                IncludeNewline = true
            });

            configureLogging(logging);
        });

        registerFrameworkServices(services);
        configureServices(services, configuration);

        return new LambdaHost(configuration, services.BuildServiceProvider());
    }
}

internal static class LambdaInvocationLifetime
{
    public static CancellationTokenSource CreateCancellationTokenSource(ILambdaContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var cancellationTokenSource = new CancellationTokenSource();
        var remaining = context.RemainingTime;

        if (remaining <= TimeSpan.Zero)
        {
            cancellationTokenSource.Cancel();
        }
        else if (remaining < TimeSpan.FromMilliseconds(int.MaxValue))
        {
            cancellationTokenSource.CancelAfter(remaining);
        }

        return cancellationTokenSource;
    }
}
