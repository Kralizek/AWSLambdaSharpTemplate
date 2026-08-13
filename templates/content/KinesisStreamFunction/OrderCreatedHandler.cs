using System.Threading;
using System.Threading.Tasks;

using Kralizek.Lambda;

using Microsoft.Extensions.Logging;

namespace LambdaFunctionProject;

public sealed class OrderCreatedHandler(ILogger<OrderCreatedHandler> logger)
    : IKinesisStreamRecordHandler<OrderCreated>
{
    public ValueTask<KinesisStreamRecordResult> HandleAsync(
        OrderCreated payload,
        KinesisStreamRecordContext context,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing order {OrderId} from partition {PartitionKey} at sequence {SequenceNumber}",
            payload.OrderId,
            context.PartitionKey,
            context.SequenceNumber);

        return ValueTask.FromResult(KinesisStreamRecordResult.Success);
    }
}
