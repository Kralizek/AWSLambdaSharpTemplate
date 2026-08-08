using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.CognitoEvents;

using Kralizek.Lambda;

using Microsoft.Extensions.Logging;

namespace LambdaFunctionProject;

public sealed class DefineAuthChallengeHandler(ILogger<DefineAuthChallengeHandler> logger) : ICognitoDefineAuthChallengeHandler
{
    public ValueTask<CognitoDefineAuthChallengeEvent> HandleAsync(
        CognitoDefineAuthChallengeEvent input,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling Cognito trigger {TriggerSource}.", input.TriggerSource);
        return ValueTask.FromResult(input);
    }
}
