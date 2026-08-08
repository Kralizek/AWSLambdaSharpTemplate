using System.Threading;
using System.Threading.Tasks;

using Kralizek.Lambda;

using Microsoft.Extensions.Logging;

namespace SqsFunction;

public sealed class OrderCreatedHandler(ILogger<OrderCreatedHandler> logger)
    : ISqsMessageHandler<OrderCreated>
{
    public ValueTask HandleAsync(
        OrderCreated message,
        SqsMessageContext context,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing order {OrderId} from SQS message {MessageId}",
            message.OrderId,
            context.MessageId);

        return ValueTask.CompletedTask;
    }
}