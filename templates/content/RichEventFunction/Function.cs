using Amazon.Lambda.Core;
using Kralizek.Lambda;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace RichEventFunction
{
    public class Function : EventFunction<string>
    {
        protected override void Configure(IConfigurationBuilder builder)
        {
            // Use this method to register your configuration flow. Exactly like in ASP.NET Core
            builder.SetBasePath(Directory.GetCurrentDirectory());
            builder.AddJsonFile("appsettings.json", optional: true);
            builder.AddEnvironmentVariables();
        }

        protected override void ConfigureLogging(ILoggingBuilder logging, IExecutionEnvironment executionEnvironment)
        {
            // Use this method to configure the logging

            logging.AddConfiguration(Configuration.GetSection("Logging"));

            /* Pushes the valid log entries into the CloudWatch log group created for this Lambda function */
            logging.AddLambdaLogger(new LambdaLoggerOptions
            {
                IncludeCategory = true,
                IncludeLogLevel = true,
                IncludeNewline = true,
                Filter = (categoryName, logLevel) =>
                {
                    /* Here you can filter which logs should go to CloudWatch. */

                    return logLevel >= LogLevel.Information;
                }
            });
        }

        protected override void ConfigureServices(IServiceCollection services, IExecutionEnvironment executionEnvironment)
        {
            // You need this line to register your handler
            RegisterHandler<StringEventHandler>(services);

            // Use this method to register your services. Exactly like in ASP.NET Core
        }
    }
}
