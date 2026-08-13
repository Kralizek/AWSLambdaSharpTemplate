using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.KinesisEvents;

using Kralizek.Lambda;

using Microsoft.Extensions.Logging;

namespace LambdaFunctionProject;

public sealed class RawKinesisStreamRecordHandler(ILogger<RawKinesisStreamRecordHandler> logger)
    : IKinesisStreamRecordHandler
{
    public ValueTask<KinesisStreamRecordResult> HandleAsync(
        KinesisEvent.KinesisEventRecord record,
        KinesisStreamRecordContext context,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing raw Kinesis record from partition {PartitionKey} at sequence {SequenceNumber}",
            context.PartitionKey,
            context.SequenceNumber);

        return ValueTask.FromResult(KinesisStreamRecordResult.Success);
    }
}
