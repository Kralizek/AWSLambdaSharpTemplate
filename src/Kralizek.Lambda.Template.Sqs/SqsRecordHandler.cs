using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.SQSEvents;

namespace Kralizek.Lambda;

/// <summary>
/// Infrastructure adapter that decodes an SQS record and dispatches it to the consumer handler.
/// </summary>
/// <remarks>
/// This type is public because it participates in the constructed base type of <see cref="SqsFunction{TMessage,THandler}"/>.
/// Consumers should implement <see cref="ISqsMessageHandler{TMessage}"/> instead.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class SqsRecordHandler<TMessage, THandler>
    : IRecordHandler<SQSEvent.SQSMessage, bool, RecordContext>
    where THandler : class, ISqsMessageHandler<TMessage>
{
    private readonly IStringPayloadDecoder<TMessage> _decoder;
    private readonly THandler _handler;

    public SqsRecordHandler(IStringPayloadDecoder<TMessage> decoder, THandler handler)
    {
        _decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public async ValueTask<bool> HandleAsync(
        SQSEvent.SQSMessage record,
        RecordContext context,
        CancellationToken cancellationToken)
    {
        var message = await _decoder.DecodeAsync(record.Body, cancellationToken).ConfigureAwait(false);
        var messageContext = SqsMessageContext.Create(context, record);

        await _handler.HandleAsync(message, messageContext, cancellationToken).ConfigureAwait(false);

        return true;
    }
}