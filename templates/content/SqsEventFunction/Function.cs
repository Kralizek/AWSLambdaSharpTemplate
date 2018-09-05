using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Kralizek.Lambda;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace SqsEventFunction
{
    public class Function : EventFunction<SQSEvent>
    {
        protected override void Configure(IConfigurationBuilder builder)
        {
            // Use this method to register your configuration flow. Exactly like in ASP.NET Core
        }

        protected override void ConfigureLogging(ILoggerFactory loggerFactory, IExecutionEnvironment executionEnvironment)
        {
            // Use this method to install logger providers
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            // You need this line to register your handler
            services.UseSqsHandler<Notification, NotificationHandler>();

            // Use this method to register your services. Exactly like in ASP.NET Core
        }
    }
}
