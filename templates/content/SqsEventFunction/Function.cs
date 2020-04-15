using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Kralizek.Lambda;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.LambdaJsonSerializer))]

namespace SqsEventFunction
{
    public class Function : EventFunction<SQSEvent>
    {
        protected override void Configure(IConfigurationBuilder builder)
        {
            // Use this method to register your configuration flow. Exactly like in ASP.NET Core
        }

        protected override void ConfigureLogging(ILoggingBuilder logging, IExecutionEnvironment executionEnvironment)
        {
            // Use this method to install logger providers
        }

        protected override void ConfigureServices(IServiceCollection services, IExecutionEnvironment executionEnvironment)
        {
            // You need this line to register your handler
            services.UseSqsHandler<TestMessage, TestMessageHandler>();

            // Use this method to register your services. Exactly like in ASP.NET Core
        }
    }
}
