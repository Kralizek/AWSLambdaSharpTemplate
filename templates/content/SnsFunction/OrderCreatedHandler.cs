using System.Threading;
using System.Threading.Tasks;

#if (aot)
using System.Text.Json.Serialization;
#endif

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

#if (aot)
[JsonSerializable(typeof(OrderCreated))]
internal partial class LambdaJsonSerializerContext;
#endif
