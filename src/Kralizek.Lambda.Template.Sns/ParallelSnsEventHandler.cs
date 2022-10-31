using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kralizek.Lambda;


/// <summary>
/// A set of options to customize the parallel execution of SNS notifications.
/// </summary>
public class ParallelSnsExecutionOptions
{
    /// <summary>
    /// The top limit of concurrent threads processing the incoming notifications. 
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
}

/// <summary>
/// An implementation of <see cref="IEventHandler{TInput}"/> specialized for <see cref="SNSEvent"/> that processes all the records in parallel.
/// </summary>
/// <typeparam name="TNotification">The internal type of the SNS notification.</typeparam>
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

    /// <summary>
    /// Handles the <see cref="SNSEvent"/> by processing each record in parallel.
    /// </summary>
    /// <param name="input">The incoming event.</param>
    /// <param name="context">The execution context.</param>
    /// <exception cref="InvalidOperationException">Thrown if there is no registered implementation of <see cref="INotificationHandler{TNotification}"/>.</exception>
    public async Task HandleAsync(SNSEvent? input, ILambdaContext context)
    {
        if (input is { Records.Count: > 0 })
        {
            await input.Records.ForEachAsync(_options.MaxDegreeOfParallelism, async record =>
            {
                using var scope = _serviceProvider.CreateScope();

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
            });
        }
    }
}