using System.Threading;
using System.Threading.Tasks;

namespace Kralizek.Lambda;

/// <summary>
/// Handles a decoded notification from an SNS record.
/// </summary>
/// <typeparam name="TNotification">The decoded notification type.</typeparam>
public interface ISnsNotificationHandler<in TNotification>
{
    /// <summary>
    /// Handles one decoded SNS notification.
    /// </summary>
    ValueTask<SnsRecordResult> HandleAsync(
        TNotification notification,
        SnsNotificationContext context,
        CancellationToken cancellationToken);
}