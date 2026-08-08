using System.Threading;
using System.Threading.Tasks;

using Kralizek.Lambda;

using Microsoft.Extensions.Logging;

namespace LambdaFunctionProject;

public class ToUpperStringRequestHandler(ILogger<ToUpperStringRequestHandler> logger) : IRequestHandler<string, string>
{
    private readonly ILogger<ToUpperStringRequestHandler> _logger = logger;

    public ValueTask<string> HandleAsync(string input, RequestContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received: {Input}", input);

        return ValueTask.FromResult(input.ToUpperInvariant());
    }
}