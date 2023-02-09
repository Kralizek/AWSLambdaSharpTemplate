using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Kralizek.Lambda;
using Microsoft.Extensions.Logging;

namespace SqsBatchResponseFunction;

public class TestMessageHandler : IMessageHandler<TestMessage>
{
    private readonly ILogger<TestMessageHandler> _logger;

    public TestMessageHandler(ILogger<TestMessageHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task HandleAsync(TestMessage? message, ILambdaContext context)
    {
        if (!string.IsNullOrWhiteSpace(message?.Message))
        {
            _logger.LogInformation("Received notification with valid message: {Message}", message?.Message);
        }
        else
        {
            throw new ArgumentException("Received notification with null or empty message", nameof(message));
        }

        return Task.CompletedTask;
    }
}