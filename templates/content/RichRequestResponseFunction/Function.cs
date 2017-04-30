using Amazon.Lambda.Core;
using Kralizek.Lambda;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace RichRequestResponseFunction
{
    public class Function : RequestResponseFunction<string, string>
    {
        protected override void Configure(IConfigurationBuilder builder)
        {
            // Use this method to register your configuration flow. Exactly like in ASP.NET Core
            builder.SetBasePath(Directory.GetCurrentDirectory());

            builder.AddJsonFile("application.json", optional: true);
            builder.AddEnvironmentVariables();
        }

        protected override void ConfigureLogging(ILoggerFactory loggerFactory, IExecutionEnvironment executionEnvironment)
        {
            // Use this method to install logger providers

            /* Pushes the valid log entries into the CloudWatch log group created for this Lambda function */
            loggerFactory.AddLambdaLogger(new LambdaLoggerOptions
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

        protected override void ConfigureServices(IServiceCollection services)
        {
            // You need this line to register your handler
            RegisterHandler<ToUpperStringRequestResponseHandler>(services);

            // Use this method to register your services. Exactly like in ASP.NET Core
        }
    }
}
