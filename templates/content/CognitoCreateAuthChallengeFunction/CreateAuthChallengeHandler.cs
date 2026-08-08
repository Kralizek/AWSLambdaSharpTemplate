using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.CognitoEvents;
using Kralizek.Lambda;
namespace LambdaFunctionProject;
public sealed class CreateAuthChallengeHandler : ICognitoCreateAuthChallengeHandler
{
    public ValueTask<CognitoCreateAuthChallengeEvent> HandleAsync(CognitoCreateAuthChallengeEvent input, RequestContext context, CancellationToken cancellationToken) => ValueTask.FromResult(input);
}
