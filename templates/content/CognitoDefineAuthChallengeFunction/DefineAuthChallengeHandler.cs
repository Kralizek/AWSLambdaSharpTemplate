using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.CognitoEvents;
using Kralizek.Lambda;
namespace LambdaFunctionProject;
public sealed class DefineAuthChallengeHandler : ICognitoDefineAuthChallengeHandler
{
    public ValueTask<CognitoDefineAuthChallengeEvent> HandleAsync(CognitoDefineAuthChallengeEvent input, RequestContext context, CancellationToken cancellationToken) => ValueTask.FromResult(input);
}
