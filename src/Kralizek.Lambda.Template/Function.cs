using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kralizek.Lambda;

public abstract class Function
{
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

    protected virtual void Configure(IConfigurationBuilder builder) { }

    protected virtual void ConfigureServices(IServiceCollection services, IExecutionEnvironment executionEnvironment) { }

    protected virtual void ConfigureLogging(ILoggingBuilder logging, IExecutionEnvironment executionEnvironment) { }

    protected IConfigurationRoot Configuration { get; }

    protected IServiceProvider ServiceProvider { get; }

    protected ILogger Logger { get; }
}