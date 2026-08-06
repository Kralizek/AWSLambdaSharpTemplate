using System.Threading;
using System.Threading.Tasks;

using Kralizek.Lambda;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Amazon.Lambda.Core;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EventFunction;

public class Function : EventFunction<string, StringEventHandler>
{
    protected override void Configure(IConfigurationBuilder builder)
    {
        builder.AddEnvironmentVariables();
    }

    protected override void ConfigureLogging(ILoggingBuilder logging, IExecutionEnvironment executionEnvironment)
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