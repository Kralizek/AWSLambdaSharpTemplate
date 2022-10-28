using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kralizek.Lambda;

public class SqsEventHandler<TMessage> : IEventHandler<SQSEvent> where TMessage : class
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
            using (var scope = _serviceProvider.CreateScope())
            {
                var sqsMessage = record.Body;

                var serializer = _serviceProvider.GetRequiredService<IMessageSerializer>();
                    
                var message = serializer.Deserialize<TMessage>(sqsMessage);

                var handler = scope.ServiceProvider.GetService<IMessageHandler<TMessage>>();

                if (handler == null)
                {
                    _logger.LogError("No {Handler} could be found", $"IMessageHandler<{typeof(TMessage).Name}>");
                    throw new InvalidOperationException($"No IMessageHandler<{typeof(TMessage).Name}> could be found.");
                }

                _logger.LogInformation("Invoking notification handler");
                await handler.HandleAsync(message, context).ConfigureAwait(false);
            }
        }
    }
}