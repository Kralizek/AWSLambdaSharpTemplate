using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Amazon.Lambda.SQSEvents.SQSBatchResponse;

namespace Kralizek.Lambda;

/// <summary>
/// An implementation of <see cref="IEventHandler{TInput}"/> specialized for <see cref="SQSEvent"/> that processes all the records in sequence.
/// Also implements <see cref="IRequestResponseHandler{TInput, TOutput}"/> to support partial batch responses.
/// </summary>
/// <typeparam name="TMessage">The internal type of the SQS message.</typeparam>
public class SqsEventHandler<TMessage> : IEventHandler<SQSEvent>, IRequestResponseHandler<SQSEvent, SQSBatchResponse> where TMessage : class
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;

    public SqsEventHandler(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory?.CreateLogger("SqsEventHandler") ?? throw new ArgumentNullException(nameof(loggerFactory));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Handles the <see cref="SQSEvent"/> by processing each record in sequence.
    /// </summary>
    /// <param name="input">The incoming event.</param>
    /// <param name="context">The execution context.</param>
    /// <exception cref="InvalidOperationException">Thrown if there is no registered implementation of <see cref="IMessageHandler{TMessage}"/>.</exception>
    public async Task HandleAsync(SQSEvent? input, ILambdaContext context) =>
        await ((IEventHandler<SQSEvent>)this).HandleAsync(input, context);

    async Task IEventHandler<SQSEvent>.HandleAsync(SQSEvent? input, ILambdaContext context) =>
        await HandleAsync(input, context, null);

    /// <summary>
    /// Handles the <see cref="SQSEvent"/> by processing each record in sequence.
    /// Catches any exceptions thrown by the <see cref="IMessageHandler{TMessage}" />, logs them, and reports them as batch response item failures.
    /// </summary>
    /// <param name="input">The incoming event.</param>
    /// <param name="context">The execution context.</param>
    /// <returns>Object conveying SQS message item failures.</returns>
    /// <seealso href="https://aws.amazon.com/about-aws/whats-new/2021/11/aws-lambda-partial-batch-response-sqs-event-source/" />
    async Task<SQSBatchResponse> IRequestResponseHandler<SQSEvent, SQSBatchResponse>.HandleAsync(SQSEvent? input, ILambdaContext context)
    {
        var batchItemFailures = new List<BatchItemFailure>();
        await HandleAsync(input, context, batchItemFailures);
        return new(batchItemFailures);
    }

    private async Task HandleAsync(SQSEvent? input, ILambdaContext context, List<BatchItemFailure>? batchItemFailures)
    {
        if (input is { Records: { } })
        {
            foreach (var record in input.Records)
            {
                using var scope = _serviceProvider.CreateScope();

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
                try
                {
                    await handler.HandleAsync(message, context).ConfigureAwait(false);
                }
                catch (Exception exc) when (batchItemFailures is not null)
                {
                    _logger.LogError(exc, "Recording batch item failure for message {MessageId}", record.MessageId);
                    batchItemFailures.Add(new() { ItemIdentifier = record.MessageId });
                }
            }
        }
    }
}