using System.Threading;
using System.Threading.Tasks;

namespace Kralizek.Lambda;

/// <summary>
/// Handles one Amazon DynamoDB Streams item change.
/// </summary>
public interface IDynamoDbStreamRecordHandler
{
    ValueTask HandleAsync(
        DynamoDbStreamItem item,
        DynamoDbStreamRecordContext context,
        CancellationToken cancellationToken);
}
