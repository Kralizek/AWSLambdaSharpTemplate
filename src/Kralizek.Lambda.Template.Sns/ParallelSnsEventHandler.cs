using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kralizek.Lambda;

public class ParallelSnsExecutionOptions
{
    public int MaxDegreeOfParallelism { get; set; } = System.Environment.ProcessorCount;
}

public class ParallelSnsEventHandler<TNotification>: IEventHandler<SNSEvent> where TNotification : class
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ParallelSnsExecutionOptions _options;

    public ParallelSnsEventHandler(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, IOptions<ParallelSnsExecutionOptions> options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = loggerFactory?.CreateLogger("SnsForEachAsyncEventHandler") ?? throw new ArgumentNullException(nameof(loggerFactory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task HandleAsync(SNSEvent input, ILambdaContext context)
    {
        if (input.Records.Any())
        {
            await input.Records.ForEachAsync(_options.MaxDegreeOfParallelism, async record =>
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var message = record.Sns.Message;

                    var serializer = _serviceProvider.GetRequiredService<INotificationSerializer>();
                    var notification = serializer.Deserialize<TNotification>(message);

                    _logger.LogDebug("Message received: {Message}", message);

                    var messageHandler = scope.ServiceProvider.GetService<INotificationHandler<TNotification>>();
                    if (messageHandler == null)
                    {
                        _logger.LogCritical("No {Handler} could be found", $"INotificationHandler<{typeof(TNotification).Name}>");
                        throw new InvalidOperationException($"No INotificationHandler<{typeof(TNotification).Name}> could be found.");
                    }
                        
                    await messageHandler.HandleAsync(notification, context);
                }
            });
        }
    }
}