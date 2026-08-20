using System;
using System.Threading;

using Amazon.Lambda.Core;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kralizek.Lambda;

/// <summary>
/// A lean hosting root for request and event functions that preserves configuration,
/// logging, dependency injection, context, and cancellation without the full processing pipeline.
/// </summary>
public abstract class MinimalLambdaFunction
{
#pragma warning disable S1699 // Configuration hooks intentionally execute during base construction. Overrides must not depend on derived-constructor state.
    protected MinimalLambdaFunction()
    {
        var host = LambdaHostBuilder.Build(
            ConfigureConfiguration,
            ConfigureLogging,
            RegisterFrameworkServices,
            ConfigureServices);

        Configuration = host.Configuration;
        ServiceProvider = host.ServiceProvider;
        Logger = ServiceProvider.GetRequiredService<ILogger<MinimalLambdaFunction>>();
    }
#pragma warning restore S1699

    /// <summary>
    /// Override to add configuration sources used by the function.
    /// </summary>
    protected virtual void ConfigureConfiguration(IConfigurationBuilder configuration) { }

    /// <summary>
    /// Registers services required by the minimal function specialization before application services are configured.
    /// </summary>
    protected virtual void RegisterFrameworkServices(IServiceCollection services) { }

    /// <summary>
    /// Override to configure additional logging providers and options.
    /// </summary>
    protected virtual void ConfigureLogging(ILoggingBuilder logging) { }

    /// <summary>
    /// Override to register application services.
    /// </summary>
    protected virtual void ConfigureServices(IServiceCollection services, IConfiguration configuration) { }

    /// <summary>
    /// The configuration built during function initialization.
    /// </summary>
    protected IConfigurationRoot Configuration { get; }

    /// <summary>
    /// The root service provider built during function initialization.
    /// </summary>
    protected IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// The function-level logger.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Creates a cancellation token source that is cancelled when the Lambda invocation runs out of time.
    /// </summary>
    protected static CancellationTokenSource CreateCancellationTokenSource(ILambdaContext context) =>
        LambdaInvocationLifetime.CreateCancellationTokenSource(context);
}
