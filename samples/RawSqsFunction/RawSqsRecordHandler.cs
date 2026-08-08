using Amazon.Lambda.SQSEvents;

using Kralizek.Lambda;

using Microsoft.Extensions.Logging;

namespace RawSqsFunction;

public sealed class RawSqsRecordHandler(ILogger<RawSqsRecordHandler> logger)
    : ISqsRecordHandler
{
    public ValueTask HandleAsync(
        SQSEvent.SQSMessage record,
        SqsMessageContext context,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing raw SQS message {MessageId} from {QueueArn}",
            context.MessageId,
            context.EventSourceArn);

        logger.LogDebug("Raw body: {Body}", record.Body);

        return ValueTask.CompletedTask;
    }
}