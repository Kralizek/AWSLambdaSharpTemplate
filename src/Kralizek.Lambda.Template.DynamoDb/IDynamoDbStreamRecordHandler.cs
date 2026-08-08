using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.DynamoDBEvents;

namespace Kralizek.Lambda;

/// <summary>
/// Handles one Amazon DynamoDB Streams record.
/// </summary>
public interface IDynamoDbStreamRecordHandler
{
    ValueTask HandleAsync(
        DynamoDBEvent.DynamodbStreamRecord record,
        DynamoDbStreamRecordContext context,
        CancellationToken cancellationToken);
}