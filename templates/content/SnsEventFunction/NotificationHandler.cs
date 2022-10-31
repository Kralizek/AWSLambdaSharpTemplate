using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Kralizek.Lambda;
using Microsoft.Extensions.Logging;

namespace SnsEventFunction;

public class NotificationHandler : INotificationHandler<Notification>
{
    private readonly ILogger<NotificationHandler> _logger;

    public NotificationHandler(ILogger<NotificationHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task HandleAsync(Notification? notification, ILambdaContext context)
    {
        _logger.LogInformation("Received notification: {Message}", notification?.Message);

        return Task.CompletedTask;
    }
}