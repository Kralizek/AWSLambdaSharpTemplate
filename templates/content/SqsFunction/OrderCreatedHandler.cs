using System.Threading;
using System.Threading.Tasks;

#if (aot)
using System.Text.Json.Serialization;
#endif

using Kralizek.Lambda;

using Microsoft.Extensions.Logging;

namespace LambdaFunctionProject;

public sealed class OrderCreatedHandler(ILogger<OrderCreatedHandler> logger)
    : ISqsMessageHandler<OrderCreated>
{
    public ValueTask<SqsRecordResult> HandleAsync(
        OrderCreated message,
        SqsMessageContext context,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing order {OrderId} from SQS message {MessageId}",
            message.OrderId,
            context.MessageId);

        return ValueTask.FromResult(SqsRecordResult.Success);
    }
}

#if (aot)
[JsonSerializable(typeof(OrderCreated))]
internal partial class PayloadJsonSerializerContext : JsonSerializerContext;
#endif
