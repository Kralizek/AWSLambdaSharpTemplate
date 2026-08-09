using System.Threading;
using System.Threading.Tasks;

using Kralizek.Lambda;

using Microsoft.Extensions.Logging;

namespace LambdaFunctionProject;

public sealed class S3BatchItemHandler(ILogger<S3BatchItemHandler> logger) : IS3BatchItemHandler
{
    private readonly ILogger<S3BatchItemHandler> _logger = logger;

    public ValueTask<S3BatchResult> HandleAsync(
        S3BatchItem item,
        S3BatchContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (item.Key is not S3BatchObjectKey objectKey)
        {
            _logger.LogWarning("Unsupported S3 Batch key type {KeyType}", item.Key.GetType().Name);

            return ValueTask.FromResult(
                S3BatchResult.PermanentFailure($"Unsupported S3 Batch key type: {item.Key.GetType().Name}"));
        }

        _logger.LogInformation(
            "Processing S3 object {Bucket}/{Key} for Batch task {TaskId}",
            objectKey.Object.Bucket,
            objectKey.Object.Key,
            context.TaskId);

        // Process objectKey.Object here.
        _ = objectKey.Object;

        return ValueTask.FromResult(S3BatchResult.Succeeded());
    }
}