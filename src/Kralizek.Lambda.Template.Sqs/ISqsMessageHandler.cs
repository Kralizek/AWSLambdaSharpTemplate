using System.Threading;
using System.Threading.Tasks;

namespace Kralizek.Lambda;

/// <summary>
/// The contract for handlers that process a single decoded SQS message.
/// </summary>
/// <typeparam name="TMessage">The decoded message type.</typeparam>
public interface ISqsMessageHandler<in TMessage>
{
    ValueTask<SqsRecordResult> HandleAsync(
        TMessage message,
        SqsMessageContext context,
        CancellationToken cancellationToken);
}