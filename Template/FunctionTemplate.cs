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
        }

        protected virtual void Configure(IConfigurationBuilder builder) { }

        protected virtual void ConfigureServices(IServiceCollection services) { }

        protected virtual void ConfigureExecution(ILambdaContext lambdaContext, ILoggerFactory loggerFactory) { }

        protected IConfigurationRoot Configuration { get; }
    }
}
