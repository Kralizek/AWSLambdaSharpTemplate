using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kralizek.Lambda;

/// <summary>
/// An implementation of <see cref="IEventHandler{TInput}"/> specialized for <see cref="SNSEvent"/> that processes all the records in sequence.
/// </summary>
/// <typeparam name="TNotification">The internal type of the SNS notification.</typeparam>
public class SnsEventHandler<TNotification> : IEventHandler<SNSEvent> where TNotification : class
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;

    public SnsEventHandler(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory?.CreateLogger("SnsEventHandler") ?? throw new ArgumentNullException(nameof(loggerFactory));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Handles the <see cref="SNSEvent"/> by processing each record in sequence.
    /// </summary>
    /// <param name="input">The incoming event.</param>
    /// <param name="context">The execution context.</param>
    /// <exception cref="InvalidOperationException">Thrown if there is no registered implementation of <see cref="INotificationHandler{TNotification}"/>.</exception>
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