using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kralizek.Lambda
{
    public abstract class Function
    {
        protected Function()
        {
            var builder = new ConfigurationBuilder();

            Configure(builder);

            Configuration = builder.Build();

            var services = new ServiceCollection();

            services.AddLogging();
            services.AddSingleton<IExecutionEnvironment>(sp => new LambdaExecutionEnvironment
            {
                EnvironmentName = Configuration["Environment"],
                IsLambda = Configuration["LAMBDA_RUNTIME_DIR"] != null
            });

            ConfigureServices(services);

            ServiceProvider = services.BuildServiceProvider();

            var loggerFactory = ServiceProvider.GetRequiredService<ILoggerFactory>();
            var executionEnvironment = ServiceProvider.GetRequiredService<IExecutionEnvironment>();

            ConfigureLogging(loggerFactory, executionEnvironment);

            Logger = loggerFactory.CreateLogger("Function");
        }

        protected virtual void Configure(IConfigurationBuilder builder) { }

        protected virtual void ConfigureServices(IServiceCollection services) { }

        protected virtual void ConfigureLogging(ILoggerFactory loggerFactory, IExecutionEnvironment executionEnvironment) { }

        protected IConfigurationRoot Configuration { get; }

        protected IServiceProvider ServiceProvider { get; }

        protected ILogger Logger { get; }

        protected IServiceScope CreateScope()
        {
            return ServiceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        }
    }
}
