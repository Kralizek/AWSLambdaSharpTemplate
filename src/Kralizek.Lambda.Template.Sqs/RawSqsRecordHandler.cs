using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.SQSEvents;

namespace Kralizek.Lambda;

/// <summary>
/// Infrastructure adapter that dispatches a raw SQS record to the consumer handler.
/// </summary>
/// <remarks>
/// This type is public because it participates in the constructed base type of <see cref="SqsFunction{THandler}"/>.
/// Consumers should implement <see cref="ISqsRecordHandler"/> instead.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class RawSqsRecordHandler<THandler>
    : IRecordHandler<SQSEvent.SQSMessage, SqsRecordResult, RecordContext>
    where THandler : class, ISqsRecordHandler
{
    private readonly THandler _handler;

    public RawSqsRecordHandler(THandler handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public ValueTask<SqsRecordResult> HandleAsync(
        SQSEvent.SQSMessage record,
        RecordContext context,
        CancellationToken cancellationToken)
    {
        var messageContext = SqsMessageContext.Create(context, record);
        return _handler.HandleAsync(record, messageContext, cancellationToken);
    }
}