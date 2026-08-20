using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kralizek.Lambda;

/// <summary>
/// The base class for all Lambda function types. Provides dependency injection, configuration, logging, and shared invocation infrastructure.
/// </summary>
public abstract class LambdaFunction
{
#pragma warning disable S1699 // Configuration hooks intentionally execute during base construction. Overrides must not depend on derived-constructor state.
    protected LambdaFunction()
    {
        var host = LambdaHostBuilder.Build(
            ConfigureConfiguration,
            ConfigureLogging,
            RegisterFrameworkServices,
            ConfigureServices);

        Configuration = host.Configuration;
        ServiceProvider = host.ServiceProvider;
        Logger = ServiceProvider.GetRequiredService<ILogger<LambdaFunction>>();
    }
#pragma warning restore S1699

    /// <summary>
    /// Override to add configuration sources used by the function.
    /// </summary>
    /// <remarks>
    /// This method is invoked while the <see cref="LambdaFunction"/> base constructor is executing.
    /// Overrides must not depend on state initialized by a derived-class constructor.
    /// </remarks>
    protected virtual void ConfigureConfiguration(IConfigurationBuilder configuration) { }

    /// <summary>
    /// Registers services required by the function model before application services are configured.
    /// </summary>
    /// <remarks>
    /// Function model roots seal this pipeline and expose their own independent
    /// <c>ConfigureFrameworkServices</c> customization hooks.
    /// This method is invoked while the <see cref="LambdaFunction"/> base constructor is executing.
    /// Overrides must not depend on state initialized by a derived-class constructor.
    /// </remarks>
    protected virtual void RegisterFrameworkServices(IServiceCollection services) { }

    /// <summary>
    /// Override to configure additional logging providers and options.
    /// </summary>
    /// <remarks>
    /// Lambda-compatible logging is configured by default. This method is invoked while the
    /// <see cref="LambdaFunction"/> base constructor is executing. Overrides must not depend on
    /// state initialized by a derived-class constructor.
    /// </remarks>
    protected virtual void ConfigureLogging(ILoggingBuilder logging) { }

    /// <summary>
    /// Override to register application services.
    /// </summary>
    /// <remarks>
    /// This method is invoked while the <see cref="LambdaFunction"/> base constructor is executing.
    /// Overrides must not depend on state initialized by a derived-class constructor.
    /// </remarks>
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
    /// Resolves and executes a handler from the supplied service provider.
    /// </summary>
    protected async ValueTask ExecuteHandlerAsync<THandler>(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken,
        Func<THandler, CancellationToken, ValueTask> invocation)
        where THandler : notnull
    {
        cancellationToken.ThrowIfCancellationRequested();

        var handler = serviceProvider.GetRequiredService<THandler>();

        Logger.LogDebug("Invoking handler {Handler}", typeof(THandler).Name);

        await invocation(handler, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Resolves and executes a handler from the supplied service provider and returns its result.
    /// </summary>
    protected async ValueTask<TResult> ExecuteHandlerAsync<THandler, TResult>(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken,
        Func<THandler, CancellationToken, ValueTask<TResult>> invocation)
        where THandler : notnull
    {
        cancellationToken.ThrowIfCancellationRequested();

        var handler = serviceProvider.GetRequiredService<THandler>();

        Logger.LogDebug("Invoking handler {Handler}", typeof(THandler).Name);

        return await invocation(handler, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a <see cref="CancellationTokenSource"/> that is cancelled when the Lambda invocation runs out of time.
    /// </summary>
    protected static CancellationTokenSource CreateCancellationTokenSource(ILambdaContext context) =>
        LambdaInvocationLifetime.CreateCancellationTokenSource(context);
}
