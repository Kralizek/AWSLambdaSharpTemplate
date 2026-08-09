using System.Threading;
using System.Threading.Tasks;

using Kralizek.Lambda;

using Microsoft.Extensions.Logging;

namespace DynamoDbStreamFunction;

public sealed class OrderChangeHandler(ILogger<OrderChangeHandler> logger)
    : IDynamoDbStreamRecordHandler
{
    public ValueTask<DynamoDbStreamRecordResult> HandleAsync(
        DynamoDbStreamItem item,
        DynamoDbStreamRecordContext context,
        CancellationToken cancellationToken)
    {
        var orderId = item.Keys.TryGetValue("orderId", out var orderIdAttribute)
            ? orderIdAttribute.S
            : null;

        logger.LogInformation(
            "Processing DynamoDB {EventName} for order {OrderId} at sequence {SequenceNumber}",
            context.EventName,
            orderId,
            item.SequenceNumber);

        return ValueTask.FromResult(DynamoDbStreamRecordResult.Success);
    }
}