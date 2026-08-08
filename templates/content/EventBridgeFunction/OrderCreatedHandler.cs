using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.CloudWatchEvents;

using Kralizek.Lambda;

using Microsoft.Extensions.Logging;

namespace LambdaFunctionProject;

public sealed class OrderCreatedHandler(ILogger<OrderCreatedHandler> logger)
    : IEventBridgeHandler<OrderCreated>
{
    public ValueTask HandleAsync(
        CloudWatchEvent<OrderCreated> input,
        EventContext context,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing order {OrderId} from {Source} ({DetailType})",
            input.Detail.OrderId,
            input.Source,
            input.DetailType);

        return ValueTask.CompletedTask;
    }
}
