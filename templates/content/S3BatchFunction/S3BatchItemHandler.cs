using System.Threading;
using System.Threading.Tasks;

using Kralizek.Lambda;

namespace LambdaFunctionProject;

public sealed class S3BatchItemHandler : IS3BatchItemHandler
{
#pragma warning disable S2325 // Interface implementation must remain an instance method.
    public ValueTask<S3BatchResult> HandleAsync(
        S3BatchItem item,
        S3BatchContext context,
        CancellationToken cancellationToken)
#pragma warning restore S2325
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