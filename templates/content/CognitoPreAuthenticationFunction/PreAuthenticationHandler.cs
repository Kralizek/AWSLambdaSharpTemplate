using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.CognitoEvents;
using Kralizek.Lambda;
namespace LambdaFunctionProject;
public sealed class PreAuthenticationHandler : ICognitoPreAuthenticationHandler
{
    public ValueTask<CognitoPreAuthenticationEvent> HandleAsync(CognitoPreAuthenticationEvent input, RequestContext context, CancellationToken cancellationToken) => ValueTask.FromResult(input);
}
