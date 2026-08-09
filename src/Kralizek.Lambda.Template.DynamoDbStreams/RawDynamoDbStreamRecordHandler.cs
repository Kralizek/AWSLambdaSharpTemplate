using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.DynamoDBEvents;

namespace Kralizek.Lambda;

/// <summary>
/// Infrastructure adapter that dispatches a raw DynamoDB Streams record to the consumer handler.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class RawDynamoDbStreamRecordHandler<THandler>
    : IRecordHandler<DynamoDBEvent.DynamodbStreamRecord, DynamoDbStreamRecordResult, RecordContext>
    where THandler : class, IDynamoDbStreamRecordHandler
{
    private readonly THandler _handler;

    public RawDynamoDbStreamRecordHandler(THandler handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public async ValueTask<DynamoDbStreamRecordResult> HandleAsync(
        DynamoDBEvent.DynamodbStreamRecord record,
        RecordContext context,
        CancellationToken cancellationToken)
    {
        var item = DynamoDbStreamItem.Create(record);
        var recordContext = DynamoDbStreamRecordContext.Create(context, record);

        await _handler.HandleAsync(item, recordContext, cancellationToken).ConfigureAwait(false);

        return DynamoDbStreamRecordResult.Completed;
    }
}
