using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.CognitoEvents;
using Kralizek.Lambda;
namespace LambdaFunctionProject;
public sealed class VerifyAuthChallengeHandler : ICognitoVerifyAuthChallengeHandler
{
    public ValueTask<CognitoVerifyAuthChallengeEvent> HandleAsync(CognitoVerifyAuthChallengeEvent input, RequestContext context, CancellationToken cancellationToken) => ValueTask.FromResult(input);
}
