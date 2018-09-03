using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Kralizek.Lambda
{
    public class SqsEventHandler<TNotification> : IEventHandler<SQSEvent> where TNotification : class
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;

        public SqsEventHandler(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory?.CreateLogger("SqsEventHandler") ?? throw new ArgumentNullException(nameof(loggerFactory));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task HandleAsync(SQSEvent input, ILambdaContext context)
        {
            foreach (var record in input.Records)
            {
                using (_serviceProvider.CreateScope())
                {
                    var message = record.Body;
                    var notification = JsonConvert.DeserializeObject<TNotification>(message);

                    var handler = _serviceProvider.GetService<INotificationHandler<TNotification>>();

                    if (handler == null)
                    {
                        _logger.LogCritical($"No INotificationHandler<{typeof(TNotification).Name}> could be found.");
                        throw new InvalidOperationException($"No INotificationHandler<{typeof(TNotification).Name}> could be found.");
                    }

                    _logger.LogInformation("Invoking notification handler");
                    await handler.HandleAsync(notification, context);
                }
            }
        }
    }
}
