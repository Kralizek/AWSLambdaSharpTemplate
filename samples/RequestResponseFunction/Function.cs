using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Kralizek.Lambda;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace RequestResponseFunction
{
    public class Function : RequestResponseFunction<string ,string>
    {
        protected override void Configure(IConfigurationBuilder builder)
        {
            builder.AddEnvironmentVariables();
        }

        protected override void ConfigureLogging(ILoggerFactory loggingFactory)
        {
            loggingFactory.AddLambdaLogger(new LambdaLoggerOptions
            {
                IncludeCategory = true,
                IncludeLogLevel = true,
                IncludeNewline = true
            });
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            RegisterHandler<Handler>(services);
        }
    }

    public class Handler : IRequestResponseHandler<string, string>
    {
        private readonly ILogger<EventHandler> _logger;

        public Handler(ILogger<EventHandler> logger)
        {
            _logger = logger;
        }

        public async Task<string> HandleAsync(string input)
        {
            _logger.LogInformation(input);
            return input?.ToUpper();
        }
    }

}
