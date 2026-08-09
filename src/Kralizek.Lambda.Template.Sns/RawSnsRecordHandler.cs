using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.SNSEvents;

namespace Kralizek.Lambda;

/// <summary>
/// Infrastructure adapter that dispatches a raw SNS record to the consumer handler.
/// </summary>
/// <remarks>
/// This type is public because it participates in the constructed base type of <see cref="SnsFunction{THandler}"/>.
/// Consumers should implement <see cref="ISnsRecordHandler"/> instead.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class RawSnsRecordHandler<THandler>
    : IRecordHandler<SNSEvent.SNSRecord, SnsRecordResult, RecordContext>
    where THandler : class, ISnsRecordHandler
{
    private readonly THandler _handler;

    public RawSnsRecordHandler(THandler handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public async ValueTask<SnsRecordResult> HandleAsync(
        SNSEvent.SNSRecord record,
        RecordContext context,
        CancellationToken cancellationToken)
    {
        var notificationContext = SnsNotificationContext.Create(context, record);

        await _handler.HandleAsync(record, notificationContext, cancellationToken).ConfigureAwait(false);

        return SnsRecordResult.Completed;
    }
}
