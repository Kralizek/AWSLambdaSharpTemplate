using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.KinesisEvents;

namespace Kralizek.Lambda;

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class KinesisStreamRecordHandler<TPayload, THandler>
    : IRecordHandler<KinesisEvent.KinesisEventRecord, KinesisStreamRecordResult, RecordContext>
    where THandler : class, IKinesisStreamRecordHandler<TPayload>
{
    private readonly IBinaryPayloadDecoder<TPayload> _decoder;
    private readonly THandler _handler;

    public KinesisStreamRecordHandler(IBinaryPayloadDecoder<TPayload> decoder, THandler handler)
    {
        _decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public async ValueTask<KinesisStreamRecordResult> HandleAsync(
        KinesisEvent.KinesisEventRecord record,
        RecordContext context,
        CancellationToken cancellationToken)
    {
        var data = record.Kinesis?.Data?.ToArray() ?? Array.Empty<byte>();
        var payload = await _decoder.DecodeAsync(data, cancellationToken).ConfigureAwait(false);
        var recordContext = KinesisStreamRecordContext.Create(context, record);

        return await _handler.HandleAsync(payload, recordContext, cancellationToken).ConfigureAwait(false);
    }
}