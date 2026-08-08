using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.DynamoDBEvents;

using Kralizek.Lambda;

using Microsoft.Extensions.Logging;

namespace DynamoDbStreamFunction;

public sealed class OrderChangeHandler(ILogger<OrderChangeHandler> logger)
    : IDynamoDbStreamRecordHandler
{
    public ValueTask HandleAsync(
        DynamoDBEvent.DynamodbStreamRecord record,
        DynamoDbStreamRecordContext context,
        CancellationToken cancellationToken)
    {
        var orderId = context.Keys.TryGetValue("orderId", out var orderIdAttribute)
            ? orderIdAttribute.S
            : null;

        logger.LogInformation(
            "Processing DynamoDB {EventName} for order {OrderId} at sequence {SequenceNumber}",
            context.EventName,
            orderId,
            context.SequenceNumber);

        return ValueTask.CompletedTask;
    }
}