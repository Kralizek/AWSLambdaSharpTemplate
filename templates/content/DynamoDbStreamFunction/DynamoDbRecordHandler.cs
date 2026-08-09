using System.Threading;
using System.Threading.Tasks;

using Kralizek.Lambda;

using Microsoft.Extensions.Logging;

namespace LambdaFunctionProject;

public sealed class DynamoDbRecordHandler(ILogger<DynamoDbRecordHandler> logger)
    : IDynamoDbStreamRecordHandler
{
    public ValueTask<DynamoDbStreamRecordResult> HandleAsync(
        DynamoDbStreamItem item,
        DynamoDbStreamRecordContext context,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing DynamoDB {EventName} record {EventId} at sequence {SequenceNumber}",
            context.EventName,
            context.EventId,
            item.SequenceNumber);

        // Keys, NewImage and OldImage expose the AWS DynamoDB AttributeValue model directly.
        // Add application-specific mapping here when a strongly typed domain model is useful.

        return ValueTask.FromResult(DynamoDbStreamRecordResult.Success);
    }
}