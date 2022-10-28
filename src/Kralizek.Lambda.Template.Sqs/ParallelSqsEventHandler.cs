using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kralizek.Lambda
{
    public class ParallelSqsExecutionOptions
    {
        public int MaxDegreeOfParallelism { get; set; } = System.Environment.ProcessorCount;
    }

    public class ParallelSqsEventHandler<TMessage>: IEventHandler<SQSEvent> where TMessage : class
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ParallelSqsExecutionOptions _options;

        public ParallelSqsEventHandler(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, IOptions<ParallelSqsExecutionOptions> options)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = loggerFactory?.CreateLogger("SqsForEachAsyncEventHandler") ??
                      throw new ArgumentNullException(nameof(loggerFactory));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task HandleAsync(SQSEvent input, ILambdaContext context)
        {
            if (input.Records.Any())
            {
                await input.Records.ForEachAsync(_options.MaxDegreeOfParallelism, async singleSqsMessage =>
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var sqsMessage = singleSqsMessage.Body;
                        _logger.LogDebug("Message received: {Message}", sqsMessage);

                        var serializer = _serviceProvider.GetRequiredService<IMessageSerializer>();
                        var message = serializer != null
                            ? serializer.Deserialize<TMessage>(sqsMessage)
                            : JsonSerializer.Deserialize<TMessage>(sqsMessage);

                        var messageHandler = scope.ServiceProvider.GetService<IMessageHandler<TMessage>>();

                        if (messageHandler == null)
                        {
                            _logger.LogError("No {Handler} could be found", $"IMessageHandler<{typeof(TMessage).Name}>");
                            throw new InvalidOperationException($"No IMessageHandler<{typeof(TMessage).Name}> could be found.");
                        }

                        await messageHandler.HandleAsync(message, context);
                    }
                });
            }
        }
    }
}