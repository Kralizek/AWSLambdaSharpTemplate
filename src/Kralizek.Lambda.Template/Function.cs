using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kralizek.Lambda;

/// <summary>
/// A base class for all functions
/// </summary>
public abstract class Function
{
    /// <summary>
    /// The base constructor for all classes. This constructor is responsible for initializing the function.
    /// </summary>
    protected Function()
    {
        var services = new ServiceCollection();

        var builder = new ConfigurationBuilder();
            
        Configure(builder);

        Configuration = builder.Build();

        var executionEnvironment = new LambdaExecutionEnvironment
        {
            EnvironmentName = Configuration["Environment"],
            IsLambda = Configuration["LAMBDA_RUNTIME_DIR"] != null
        };

        services.AddSingleton<IExecutionEnvironment>(executionEnvironment);

        services.AddSingleton(Configuration);

        services.AddLogging(logging => ConfigureLogging(logging, executionEnvironment));

        ConfigureServices(services, executionEnvironment);

        ServiceProvider = services.BuildServiceProvider();

        Logger = ServiceProvider.GetRequiredService<ILogger<Function>>();
    }

    /// <summary>
    /// Use this method to register your configuration flow. Exactly like in ASP.NET Core.
    /// </summary>
    protected virtual void Configure(IConfigurationBuilder builder) { }

    /// <summary>
    /// Use this method to register your services. Exactly like in ASP.NET Core.
    /// </summary>
    protected virtual void ConfigureServices(IServiceCollection services, IExecutionEnvironment executionEnvironment) { }

    /// <summary>
    /// Use this method to configure the logging. Exactly like in ASP.NET Core.
    /// </summary>
    protected virtual void ConfigureLogging(ILoggingBuilder logging, IExecutionEnvironment executionEnvironment) { }

    /// <summary>
    /// The root configuration.
    /// </summary>
    protected IConfigurationRoot Configuration { get; }

    /// <summary>
    /// The service provider.
    /// </summary>
    protected IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// The default logger for the function.
    /// </summary>
    protected ILogger Logger { get; }
}