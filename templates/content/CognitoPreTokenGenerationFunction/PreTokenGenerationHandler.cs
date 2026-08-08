using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.CognitoEvents;

using Kralizek.Lambda;

using Microsoft.Extensions.Logging;

namespace LambdaFunctionProject;

#if (preTokenV2)
public sealed class PreTokenGenerationHandler(ILogger<PreTokenGenerationHandler> logger) : ICognitoPreTokenGenerationV2Handler
{
    public ValueTask<CognitoPreTokenGenerationV2Event> HandleAsync(
        CognitoPreTokenGenerationV2Event input,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling Cognito trigger {TriggerSource}.", input.TriggerSource);
        return ValueTask.FromResult(input);
    }
}
#else
public sealed class PreTokenGenerationHandler(ILogger<PreTokenGenerationHandler> logger) : ICognitoPreTokenGenerationHandler
{
    public ValueTask<CognitoPreTokenGenerationEvent> HandleAsync(
        CognitoPreTokenGenerationEvent input,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling Cognito trigger {TriggerSource}.", input.TriggerSource);
        return ValueTask.FromResult(input);
    }
}
#endif
