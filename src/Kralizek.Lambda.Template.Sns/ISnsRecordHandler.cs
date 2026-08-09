using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.SNSEvents;

namespace Kralizek.Lambda;

/// <summary>
/// Handles a raw SNS record.
/// </summary>
public interface ISnsRecordHandler
{
    /// <summary>
    /// Handles one SNS record without decoding its message payload.
    /// </summary>
    ValueTask<SnsRecordResult> HandleAsync(
        SNSEvent.SNSRecord record,
        SnsNotificationContext context,
        CancellationToken cancellationToken);
}