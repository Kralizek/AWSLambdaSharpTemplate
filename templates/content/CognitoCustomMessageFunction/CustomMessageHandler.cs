using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.CognitoEvents;
using Kralizek.Lambda;
namespace LambdaFunctionProject;
public sealed class CustomMessageHandler : ICognitoCustomMessageHandler
{
    public ValueTask<CognitoCustomMessageEvent> HandleAsync(CognitoCustomMessageEvent input, RequestContext context, CancellationToken cancellationToken) => ValueTask.FromResult(input);
}
