using System.Threading;
using System.Threading.Tasks;

namespace Kralizek.Lambda;

/// <summary>
/// Routes payloads to tenant-isolated AWS Lambda functions.
/// </summary>
public interface ITenantLambdaRouter
{
    /// <summary>
    /// Invokes the downstream Lambda synchronously for the route's tenant.
    /// </summary>
    /// <remarks>
    /// Successful completion means the downstream function completed without reporting a Lambda function error.
    /// </remarks>
    ValueTask RouteAsync(
        TenantLambdaRoute route,
        CancellationToken cancellationToken = default);
}
