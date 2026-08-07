using System.Threading;
using System.Threading.Tasks;

using Kralizek.Lambda;

using Microsoft.Extensions.Logging;

namespace LambdaFunctionProject;

public class StringEventHandler(ILogger<StringEventHandler> logger) : IEventHandler<string>
{
    private readonly ILogger<StringEventHandler> _logger = logger;

    public ValueTask HandleAsync(string input, EventContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received: {Input}", input);

        return ValueTask.CompletedTask;
    }
}