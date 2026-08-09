using System.Threading;
using System.Threading.Tasks;

namespace Kralizek.Lambda;

/// <summary>
/// Handles one Amazon DynamoDB Streams item change.
/// </summary>
public interface IDynamoDbStreamRecordHandler
{
    ValueTask<DynamoDbStreamRecordResult> HandleAsync(
        DynamoDbStreamItem item,
        DynamoDbStreamRecordContext context,
        CancellationToken cancellationToken);
}