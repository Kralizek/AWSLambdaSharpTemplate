using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.CognitoEvents;
using Kralizek.Lambda;
namespace LambdaFunctionProject;
public sealed class PostAuthenticationHandler : ICognitoPostAuthenticationHandler
{
    public ValueTask<CognitoPostAuthenticationEvent> HandleAsync(CognitoPostAuthenticationEvent input, RequestContext context, CancellationToken cancellationToken) => ValueTask.FromResult(input);
}
