using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Kralizek.Lambda
{
    public class SnsEventHandler<TNotification> : IEventHandler<SNSEvent> where TNotification : class
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;

        public SnsEventHandler(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory?.CreateLogger("SnsEventHandler") ?? throw new ArgumentNullException(nameof(loggerFactory));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task HandleAsync(SNSEvent input, ILambdaContext context)
        {
            foreach (var record in input.Records)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var message = record.Sns.Message;
                    var notification = JsonSerializer.Deserialize<TNotification>(message);
                    
                    var handler = scope.ServiceProvider.GetService<INotificationHandler<TNotification>>();

                    if (handler == null)
                    {
                        _logger.LogCritical($"No INotificationHandler<{typeof(TNotification).Name}> could be found.");
                        throw new InvalidOperationException($"No INotificationHandler<{typeof(TNotification).Name}> could be found.");
                    }

                    _logger.LogInformation("Invoking notification handler");
                    await handler.HandleAsync(notification, context).ConfigureAwait(false);
                }
            }
        }
    }
}
