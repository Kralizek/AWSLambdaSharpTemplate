using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.SNSEvents;

namespace Kralizek.Lambda;

/// <summary>
/// Infrastructure adapter that decodes an SNS message and dispatches it to the consumer handler.
/// </summary>
/// <remarks>
/// This type is public because it participates in the constructed base type of <see cref="SnsFunction{TNotification,THandler}"/>.
/// Consumers should implement <see cref="ISnsNotificationHandler{TNotification}"/> instead.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class SnsRecordHandler<TNotification, THandler>
    : IRecordHandler<SNSEvent.SNSRecord, SnsRecordResult, RecordContext>
    where THandler : class, ISnsNotificationHandler<TNotification>
{
    private readonly IStringPayloadDecoder<TNotification> _decoder;
    private readonly THandler _handler;

    public SnsRecordHandler(IStringPayloadDecoder<TNotification> decoder, THandler handler)
    {
        _decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public async ValueTask<SnsRecordResult> HandleAsync(
        SNSEvent.SNSRecord record,
        RecordContext context,
        CancellationToken cancellationToken)
    {
        var snsMessage = record.Sns ?? throw new InvalidOperationException("The SNS record does not contain an SNS message.");
        var notification = await _decoder.DecodeAsync(snsMessage.Message, cancellationToken).ConfigureAwait(false);
        var notificationContext = SnsNotificationContext.Create(context, record);

        await _handler.HandleAsync(notification, notificationContext, cancellationToken).ConfigureAwait(false);

        return SnsRecordResult.Completed;
    }
}
