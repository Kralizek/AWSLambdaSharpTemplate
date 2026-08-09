using System.Threading;
using System.Threading.Tasks;

using Kralizek.Lambda;

namespace LambdaFunctionProject;

public sealed class S3BatchItemHandler : IS3BatchItemHandler
{
    public ValueTask<S3BatchResult> HandleAsync(
        S3BatchItem item,
        S3BatchContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (item.Key is not S3BatchObjectKey objectKey)
        {
            return ValueTask.FromResult(
                S3BatchResult.PermanentFailure($"Unsupported S3 Batch key type: {item.Key.GetType().Name}"));
        }

        // Process objectKey.Object here.
        _ = objectKey.Object;

        return ValueTask.FromResult(S3BatchResult.Succeeded());
    }
}
