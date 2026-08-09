using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.KinesisEvents;

namespace Kralizek.Lambda;

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class RawKinesisStreamRecordHandler<THandler>
    : IRecordHandler<KinesisEvent.KinesisEventRecord, KinesisStreamRecordResult, RecordContext>
    where THandler : class, IKinesisStreamRecordHandler
{
    private readonly THandler _handler;

    public RawKinesisStreamRecordHandler(THandler handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public ValueTask<KinesisStreamRecordResult> HandleAsync(
        KinesisEvent.KinesisEventRecord record,
        RecordContext context,
        CancellationToken cancellationToken) =>
        _handler.HandleAsync(record, KinesisStreamRecordContext.Create(context, record), cancellationToken);
}