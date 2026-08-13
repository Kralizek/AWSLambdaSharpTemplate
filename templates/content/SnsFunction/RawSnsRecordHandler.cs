using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.SNSEvents;

using Kralizek.Lambda;

using Microsoft.Extensions.Logging;

namespace LambdaFunctionProject;

public sealed class RawSnsRecordHandler(ILogger<RawSnsRecordHandler> logger) : ISnsRecordHandler
{
    public ValueTask<SnsRecordResult> HandleAsync(
        SNSEvent.SNSRecord record,
        SnsNotificationContext context,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing raw SNS message {MessageId} from topic {TopicArn}",
            context.MessageId,
            context.TopicArn);

        return ValueTask.FromResult(SnsRecordResult.Completed);
    }
}
