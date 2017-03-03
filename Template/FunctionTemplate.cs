using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Template
{
    public abstract class FunctionTemplate
    {
        

        protected FunctionTemplate()
        {
            ConfigurationBuilder builder = new ConfigurationBuilder();
            Configure(builder);

            Configuration = builder.Build();

            ServiceCollection services = new ServiceCollection();
            services.AddLogging();

            ConfigureServices(services);

            ServiceProvider = services.BuildServiceProvider();

            var loggerFactory = ServiceProvider.GetRequiredService<ILoggerFactory>();

            ConfigureLogging(loggerFactory);

            Logger = loggerFactory.CreateLogger("Function");
        }
        
        protected virtual void Configure(IConfigurationBuilder builder) { }

        protected virtual void ConfigureServices(IServiceCollection services) { }

        protected virtual void ConfigureExecution(ILambdaContext lambdaContext) { }

        protected virtual void ConfigureLogging(ILoggerFactory loggerFactory) { }

        protected IConfigurationRoot Configuration { get; }

        protected IServiceProvider ServiceProvider { get; }

        protected ILogger Logger { get; }

        protected IServiceScope CreateScope()
        {
            return ServiceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        }
    }
}
