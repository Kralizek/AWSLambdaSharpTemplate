using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.SQSEvents;

using Kralizek.Lambda;

using Microsoft.Extensions.Logging;

namespace LambdaFunctionProject;

public sealed class RawSqsRecordHandler(ILogger<RawSqsRecordHandler> logger)
    : ISqsRecordHandler
{
    public ValueTask<SqsRecordResult> HandleAsync(
        SQSEvent.SQSMessage record,
        SqsMessageContext context,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing raw SQS message {MessageId} from {QueueArn}",
            context.MessageId,
            context.EventSourceArn);

        logger.LogDebug("Raw body: {Body}", record.Body);

        return ValueTask.FromResult(SqsRecordResult.Success);
    }
}
