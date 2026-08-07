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
    protected LambdaFunction(params Type[] handlerTypes)
    {
        var configurationBuilder = new ConfigurationBuilder();

        ConfigureConfiguration(configurationBuilder);

        Configuration = configurationBuilder.Build();

        var executionEnvironment = new LambdaExecutionEnvironment
        {
            EnvironmentName = Configuration["Environment"] ?? LambdaExecutionEnvironment.DevelopmentEnvironmentName,
            IsLambda = Configuration["LAMBDA_RUNTIME_DIR"] != null
        };

        var services = new ServiceCollection();

        services.AddSingleton<IExecutionEnvironment>(executionEnvironment);
        services.AddSingleton(Configuration);
        services.AddLogging(ConfigureLogging);

        foreach (var handlerType in handlerTypes)
        {
            services.AddTransient(handlerType);
        }

        ConfigureServices(services, Configuration);

        ServiceProvider = services.BuildServiceProvider();
        Logger = ServiceProvider.GetRequiredService<ILogger<LambdaFunction>>();
    }

    /// <summary>
    /// Override to add configuration sources used by the function.
    /// </summary>
    protected virtual void ConfigureConfiguration(IConfigurationBuilder configuration) { }

    /// <summary>
    /// Override to configure logging providers.
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
    /// Executes a handler in a fresh dependency-injection scope.
    /// </summary>
    protected async ValueTask InvokeAsync<THandler>(
        CancellationToken cancellationToken,
        Func<THandler, CancellationToken, ValueTask> invocation)
        where THandler : notnull
    {
        cancellationToken.ThrowIfCancellationRequested();

        await using var scope = ServiceProvider.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<THandler>();

        Logger.LogDebug("Invoking handler {Handler}", typeof(THandler).Name);

        await invocation(handler, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a handler in a fresh dependency-injection scope and returns its result.
    /// </summary>
    protected async ValueTask<TResult> InvokeAsync<THandler, TResult>(
        CancellationToken cancellationToken,
        Func<THandler, CancellationToken, ValueTask<TResult>> invocation)
        where THandler : notnull
    {
        cancellationToken.ThrowIfCancellationRequested();

        await using var scope = ServiceProvider.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<THandler>();

        Logger.LogDebug("Invoking handler {Handler}", typeof(THandler).Name);

        return await invocation(handler, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a <see cref="CancellationTokenSource"/> that is cancelled when the Lambda invocation runs out of time.
    /// </summary>
    protected static CancellationTokenSource CreateCancellationTokenSource(ILambdaContext context)
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