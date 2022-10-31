using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Kralizek.Lambda;
using Microsoft.Extensions.Logging;

namespace RichEventFunction;

public class StringEventHandler : IEventHandler<string>
{
    private readonly ILogger<StringEventHandler> _logger;

    public StringEventHandler(ILogger<StringEventHandler> logger)
    {
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }
        _logger = logger;
    }

    public Task HandleAsync(string? input, ILambdaContext context)
    {
        _logger.LogInformation("Received: {Input}", input);

        return Task.CompletedTask;
    }
}