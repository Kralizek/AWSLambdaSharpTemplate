using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.SQSEvents;

namespace Kralizek.Lambda;

/// <summary>
/// Handles a raw SQS record without decoding its body into an application contract.
/// </summary>
public interface ISqsRecordHandler
{
    ValueTask HandleAsync(
        SQSEvent.SQSMessage record,
        SqsMessageContext context,
        CancellationToken cancellationToken);
}