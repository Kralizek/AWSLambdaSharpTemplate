using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Kralizek.Lambda;

using Microsoft.Extensions.Logging;

namespace LambdaFunctionProject;

public sealed class OrderCreatedHandler(ILogger<OrderCreatedHandler> logger)
    : ISnsNotificationHandler<OrderCreated>
{
    public ValueTask<SnsRecordResult> HandleAsync(
        OrderCreated notification,
        SnsNotificationContext context,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing order {OrderId} from SNS message {MessageId}",
            notification.OrderId,
            context.MessageId);

        return ValueTask.FromResult(SnsRecordResult.Completed);
    }
}

[JsonSerializable(typeof(OrderCreated))]
internal partial class PayloadJsonSerializerContext : JsonSerializerContext;
