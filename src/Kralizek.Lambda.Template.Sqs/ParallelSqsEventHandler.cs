using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Amazon.Lambda.SQSEvents.SQSBatchResponse;

namespace Kralizek.Lambda;

/// <summary>
/// A set of options to customize the parallel execution of SQS messages.
/// </summary>
public class ParallelSqsExecutionOptions
{
    /// <summary>
    /// The top limit of concurrent threads processing the incoming notifications. 
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
}

/// <summary>
/// An implementation of <see cref="IEventHandler{TInput}"/> specialized for <see cref="SQSEvent"/> that processes all the records in parallel.
/// Also implements <see cref="IRequestResponseHandler{TInput, TOutput}"/>, specialized for <see cref="SQSEvent"/> and <see cref="SQSBatchResponse"/>,
/// to support parallel processing with partial batch responses.
/// </summary>
/// <typeparam name="TMessage">The internal type of the SQS message.</typeparam>
public class ParallelSqsEventHandler<TMessage> : IEventHandler<SQSEvent>, IRequestResponseHandler<SQSEvent, SQSBatchResponse> where TMessage : class
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ParallelSqsExecutionOptions _options;

    public ParallelSqsEventHandler(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, IOptions<ParallelSqsExecutionOptions> options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = loggerFactory?.CreateLogger("SqsForEachAsyncEventHandler") ?? throw new ArgumentNullException(nameof(loggerFactory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Handles the <see cref="SQSEvent"/> by processing each record in parallel.
    /// </summary>
    /// <param name="input">The incoming event.</param>
    /// <param name="context">The execution context.</param>
    /// <exception cref="InvalidOperationException">Thrown if there is no registered implementation of <see cref="IMessageHandler{TMessage}"/>.</exception>
    public async Task HandleAsync(SQSEvent? input, ILambdaContext context) =>
        await ((IEventHandler<SQSEvent>)this).HandleAsync(input, context);

    /// <inheritdoc cref="HandleAsync(SQSEvent?, ILambdaContext)"/>
    async Task IEventHandler<SQSEvent>.HandleAsync(SQSEvent? input, ILambdaContext context) =>
        await HandleAsync(input, context, null);

    /// <summary>
    /// Handles the <see cref="SQSEvent"/> by processing each record in parallel.
    /// Catches any exceptions thrown by the <see cref="IMessageHandler{TMessage}"/>, logs them, and reports them as batch response item failures.
    /// </summary>
    /// <param name="input">The incoming event.</param>
    /// <param name="context">The execution context.</param>
    /// <returns>Object conveying SQS message item failures.</returns>
    /// <exception cref="InvalidOperationException">Thrown if there is no registered implementation of <see cref="IMessageHandler{TMessage}"/>.</exception>
    /// <seealso href="https://aws.amazon.com/about-aws/whats-new/2021/11/aws-lambda-partial-batch-response-sqs-event-source/"/>
    async Task<SQSBatchResponse> IRequestResponseHandler<SQSEvent, SQSBatchResponse>.HandleAsync(SQSEvent? input, ILambdaContext context)
    {
        var batchItemFailures = new ConcurrentBag<BatchItemFailure>();
        await HandleAsync(input, context, batchItemFailures);
        return new(batchItemFailures.ToList());
    }

    private async Task HandleAsync(SQSEvent? input, ILambdaContext context, ConcurrentBag<BatchItemFailure>? batchItemFailures)
    {
        if (input is { Records.Count: > 0 })
        {
            await input.Records.ForEachAsync(_options.MaxDegreeOfParallelism, async singleSqsMessage =>
            {
                using var scope = _serviceProvider.CreateScope();

                var sqsMessage = singleSqsMessage.Body;
                _logger.LogDebug("Message received: {Message}", sqsMessage);

                var serializer = _serviceProvider.GetRequiredService<IMessageSerializer>();

                var message = serializer.Deserialize<TMessage>(sqsMessage);

                var messageHandler = scope.ServiceProvider.GetService<IMessageHandler<TMessage>>();

                if (messageHandler == null)
                {
                    _logger.LogError("No {Handler} could be found", $"IMessageHandler<{typeof(TMessage).Name}>");

                    throw new InvalidOperationException($"No IMessageHandler<{typeof(TMessage).Name}> could be found.");
                }

                try
                {
                    await messageHandler.HandleAsync(message, context).ConfigureAwait(false);
                }
                catch (Exception exc) when (batchItemFailures is not null)
                {
                    _logger.LogError(exc, "Recording batch item failure for message {MessageId}", singleSqsMessage.MessageId);
                    batchItemFailures.Add(new() { ItemIdentifier = singleSqsMessage.MessageId });
                }
            });
        }
    }
}