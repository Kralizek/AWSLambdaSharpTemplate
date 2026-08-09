using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.KinesisEvents;

namespace Kralizek.Lambda;

/// <summary>
/// Handles one raw Kinesis Streams record.
/// </summary>
public interface IKinesisStreamRecordHandler
{
    ValueTask<KinesisStreamRecordResult> HandleAsync(
        KinesisEvent.KinesisEventRecord record,
        KinesisStreamRecordContext context,
        CancellationToken cancellationToken);
}

/// <summary>
/// Handles one decoded Kinesis Streams payload.
/// </summary>
public interface IKinesisStreamRecordHandler<in TPayload>
{
    ValueTask<KinesisStreamRecordResult> HandleAsync(
        TPayload payload,
        KinesisStreamRecordContext context,
        CancellationToken cancellationToken);
}