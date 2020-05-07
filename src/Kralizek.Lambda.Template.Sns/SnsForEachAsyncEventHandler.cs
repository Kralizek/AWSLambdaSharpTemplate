using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kralizek.Lambda
{
    public class SnsForEachAsyncEventHandler<TNotification>: IEventHandler<SNSEvent> where TNotification : class
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly int _maxDegreeOfParallelism;

        public SnsForEachAsyncEventHandler(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, ForEachAsyncHandlingOption forEachAsyncHandlingOption)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = loggerFactory?.CreateLogger("SnsForEachAsyncEventHandler") ?? throw new ArgumentNullException(nameof(loggerFactory));
            _maxDegreeOfParallelism = forEachAsyncHandlingOption?.MaxDegreeOfParallelism ?? 1;
        }

        public async Task HandleAsync(SNSEvent input, ILambdaContext context)
        {
            if (input.Records.Any())
            {
                await input.Records.ForEachAsync(_maxDegreeOfParallelism, async record =>
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var message = record.Sns.Message;
                        var notification = JsonSerializer.Deserialize<TNotification>(message);
                        _logger.LogDebug($"Message received: {message}");

                        var messageHandler = scope.ServiceProvider.GetService<INotificationHandler<TNotification>>();
                        if (messageHandler == null)
                        {
                            _logger.LogCritical($"No INotificationHandler<{typeof(TNotification).Name}> could be found.");
                            throw new InvalidOperationException($"No INotificationHandler<{typeof(TNotification).Name}> could be found.");
                        }
                        
                        await messageHandler.HandleAsync(notification, context);
                    }
                });
            }
        }
    }
}
