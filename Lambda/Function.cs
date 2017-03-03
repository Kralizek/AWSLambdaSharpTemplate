using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Template;
// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Lambda
{
    public class Function : RequestResponseFunction<string, string>
    {
        protected override void Configure(IConfigurationBuilder builder)
        {
            // Here you configure the ConfigurationBuilder.
            // Example:
            //
            // builder
            //    .AddJsonFile("appsettings.json", optional: true)
            //    .AddEnvironmentVariables();
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            // Here you register the services you can use.

            // This call is needed to inform the base class which is your handler.
            services.AddRequestResponseHandler<UpperCaseHandler, string, string>();
        }

        protected override void ConfigureLogging(ILoggerFactory loggerFactory)
        {
            // Here you can register the loggers you prefer.

            loggerFactory.AddLambdaLogger(new LambdaLoggerOptions
            {
                IncludeCategory = true,
                IncludeLogLevel = true,
                IncludeNewline = true
            });
        }
    }

    public class UpperCaseHandler : IRequestResponseHandler<string, string>
    {
        private readonly ILogger<UpperCaseHandler> _logger;

        public UpperCaseHandler(ILogger<UpperCaseHandler> logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            _logger = logger;
        }

        public Task<string> HandleAsync(string input)
        {
            _logger.LogInformation($"Uppercasing '{input}'");
            return Task.FromResult(input?.ToUpper());
        }
    }
}
