using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;

using Kralizek.Lambda;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace RequestFunction;

public class Function : RequestFunction<string, string, UpperCaseHandler>
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

public class UpperCaseHandler : IRequestHandler<string, string>
{
    private readonly ILogger<UpperCaseHandler> _logger;

    public UpperCaseHandler(ILogger<UpperCaseHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask<string> HandleAsync(string input, RequestContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Input {Input} for request {AwsRequestId}", input, context.AwsRequestId);
        return new ValueTask<string>(input.ToUpperInvariant());
    }
}