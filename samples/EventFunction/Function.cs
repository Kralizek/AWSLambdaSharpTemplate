using System.Threading;

using Amazon.Lambda.Core;

using Kralizek.Lambda;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EventFunction;

public class Function : EventFunction<string, StringEventHandler>
{
    protected override void ConfigureConfiguration(IConfigurationBuilder configuration)
    {
        configuration.AddEnvironmentVariables();
    }

    protected override void ConfigureLogging(ILoggingBuilder logging)
    {
        logging.AddConfiguration(Configuration.GetSection("Logging"));
        logging.AddLambdaLogger(new LambdaLoggerOptions
        {
            IncludeCategory = true,
            IncludeLogLevel = true,
            IncludeNewline = true
        });
    }
}

public class StringEventHandler : IEventHandler<string>
{
    private readonly ILogger<StringEventHandler> _logger;

    public StringEventHandler(ILogger<StringEventHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask HandleAsync(string input, EventContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Input {Input} for request {AwsRequestId}", input, context.AwsRequestId);
        return ValueTask.CompletedTask;
    }
}