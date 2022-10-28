using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Kralizek.Lambda;
using Microsoft.Extensions.Logging;

namespace SqsEventFunction;

public class TestMessageHandler : IMessageHandler<TestMessage>
{
    private readonly ILogger<TestMessageHandler> _logger;

    public TestMessageHandler(ILogger<TestMessageHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task HandleAsync(TestMessage? message, ILambdaContext context)
    {
        _logger.LogInformation("Received notification: {Message}", message?.Message);

        return Task.CompletedTask;
    }
}