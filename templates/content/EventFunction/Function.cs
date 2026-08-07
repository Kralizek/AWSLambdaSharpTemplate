using Amazon.Lambda.Core;

using Kralizek.Lambda;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace LambdaFunctionProject;

public class Function : EventFunction<string, StringEventHandler>
{
    protected override void ConfigureConfiguration(IConfigurationBuilder configuration)
    {
        // Add application configuration sources here.
        base.ConfigureConfiguration(configuration);
    }

    protected override void ConfigureLogging(ILoggingBuilder logging)
    {
        // Add or customize application logging here. Lambda logging is configured by the framework.
        base.ConfigureLogging(logging);
    }

    protected override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register application services here. The primary handler is registered by the framework.
        base.ConfigureServices(services, configuration);
    }
}