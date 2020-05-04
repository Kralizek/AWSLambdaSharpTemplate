using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kralizek.Lambda
{
    public class SqsForEachAsyncEventHandler<TMessage>:  IEventHandler<SQSEvent> where TMessage : class
    {
        private readonly ILogger<SqsForEachAsyncEventHandler<TMessage>> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly int _maxDegreeOfParallelism;

        public SqsForEachAsyncEventHandler(IServiceProvider serviceProvider, ILogger<SqsForEachAsyncEventHandler<TMessage>> log, ForEachAsyncHandlingOption forEachAsyncHandlingOption)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = log ?? throw new ArgumentNullException(nameof(log));
            _maxDegreeOfParallelism = forEachAsyncHandlingOption?.MaxDegreeOfParallelism ?? 1;
        }

        public async Task HandleAsync(SQSEvent input, ILambdaContext context)
        {
            if (input.Records.Any())
            {
                await input.Records.ForEachAsync(_maxDegreeOfParallelism, async singleSqsMessage =>
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var sqsMessage = singleSqsMessage.Body;
                        _logger.LogDebug($"Message received: {sqsMessage}");

                        var message = JsonSerializer.Deserialize<TMessage>(sqsMessage);

                        var messageHandler = scope.ServiceProvider.GetService<IMessageHandler<TMessage>>();
                        if (messageHandler == null)
                        {
                            _logger.LogError($"No IMessageHandler<{typeof(TMessage).Name}> could be found.");
                            throw new InvalidOperationException($"No IMessageHandler<{typeof(TMessage).Name}> could be found.");
                        }

                        await messageHandler.HandleAsync(message, context);
                    }
                });
            }
        }
    }
}
