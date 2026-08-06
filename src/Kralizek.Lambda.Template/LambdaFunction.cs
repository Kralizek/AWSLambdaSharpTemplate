using System;
using System.Threading;

using Amazon.Lambda.Core;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kralizek.Lambda;

/// <summary>
/// The base class for all Lambda function types. Provides dependency injection, configuration, and logging setup.
/// </summary>
public abstract class LambdaFunction
{
    protected LambdaFunction()
    {
        var services = new ServiceCollection();
        var builder = new ConfigurationBuilder();

        Configure(builder);

        Configuration = builder.Build();

        var executionEnvironment = new LambdaExecutionEnvironment
        {
            EnvironmentName = Configuration["Environment"] ?? LambdaExecutionEnvironment.DevelopmentEnvironmentName,
            IsLambda = Configuration["LAMBDA_RUNTIME_DIR"] != null
        };

        services.AddSingleton<IExecutionEnvironment>(executionEnvironment);
        services.AddSingleton(Configuration);
        services.AddLogging(logging => ConfigureLogging(logging, executionEnvironment));

        RegisterHandlers(services);
        ConfigureServices(services, executionEnvironment);

        ServiceProvider = services.BuildServiceProvider();
        Logger = ServiceProvider.GetRequiredService<ILogger<LambdaFunction>>();
    }

    /// <summary>
    /// Override to register the application configuration sources.
    /// </summary>
    protected virtual void Configure(IConfigurationBuilder builder) { }

    /// <summary>
    /// Override to register handler types. Called before <see cref="ConfigureServices"/>.
    /// </summary>
    protected virtual void RegisterHandlers(IServiceCollection services) { }

    /// <summary>
    /// Override to register application services.
    /// </summary>
    protected virtual void ConfigureServices(IServiceCollection services, IExecutionEnvironment executionEnvironment) { }

    /// <summary>
    /// Override to configure logging providers.
    /// </summary>
    protected virtual void ConfigureLogging(ILoggingBuilder logging, IExecutionEnvironment executionEnvironment) { }

    /// <summary>
    /// The root configuration built during initialization.
    /// </summary>
    protected IConfigurationRoot Configuration { get; }

    /// <summary>
    /// The root service provider built during initialization.
    /// </summary>
    protected IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// The function-level logger.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Creates a <see cref="CancellationTokenSource"/> that cancels when the Lambda invocation
    /// has no time remaining, based on <see cref="ILambdaContext.RemainingTime"/>.
    /// </summary>
    protected static CancellationTokenSource CreateCancellationTokenSource(ILambdaContext context)
    {
        var remaining = context.RemainingTime;
        if (remaining <= TimeSpan.Zero || remaining >= TimeSpan.FromMilliseconds(int.MaxValue))
            return new CancellationTokenSource();
        return new CancellationTokenSource(remaining);
    }
}
