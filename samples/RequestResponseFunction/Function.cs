using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;

using Kralizek.Lambda;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace RequestResponseFunction;

public class Function : RequestFunction<string, string, UpperCaseHandler>
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

public class UpperCaseHandler : IRequestHandler<string, string>
{
    private readonly ILogger<UpperCaseHandler> _logger;

    public UpperCaseHandler(ILogger<UpperCaseHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask<string> HandleAsync(string input, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Input: {Input}", input);
        return new ValueTask<string>(input.ToUpperInvariant());
    }
}