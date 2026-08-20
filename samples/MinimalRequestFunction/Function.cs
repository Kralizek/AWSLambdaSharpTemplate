using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;

using Kralizek.Lambda;

using Microsoft.Extensions.Logging;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace MinimalRequestFunction;

public class Function : MinimalRequestFunction<string, string, UpperCaseHandler>;

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
