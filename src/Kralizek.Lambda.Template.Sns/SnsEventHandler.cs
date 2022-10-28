using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kralizek.Lambda;

public class SnsEventHandler<TNotification> : IEventHandler<SNSEvent> where TNotification : class
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;

    public SnsEventHandler(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory?.CreateLogger("SnsEventHandler") ?? throw new ArgumentNullException(nameof(loggerFactory));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task HandleAsync(SNSEvent? input, ILambdaContext context)
    {
        if (input is { Records: { } })
        {
            foreach (var record in input.Records)
            {
                using var scope = _serviceProvider.CreateScope();

                var message = record.Sns.Message;

                var serializer = _serviceProvider.GetRequiredService<INotificationSerializer>();

                var notification = serializer.Deserialize<TNotification>(message);

                var handler = scope.ServiceProvider.GetService<INotificationHandler<TNotification>>();

                if (handler == null)
                {
                    _logger.LogCritical("No {Handler} could be found", $"INotificationHandler<{typeof(TNotification).Name}>");

                    throw new InvalidOperationException($"No INotificationHandler<{typeof(TNotification).Name}> could be found.");
                }

                _logger.LogInformation("Invoking notification handler");
                await handler.HandleAsync(notification, context).ConfigureAwait(false);
            }
        }
    }
}