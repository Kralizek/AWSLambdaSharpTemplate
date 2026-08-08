using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.CognitoEvents;
using Kralizek.Lambda;
namespace LambdaFunctionProject;
#if (preTokenV2)
public sealed class PreTokenGenerationHandler : ICognitoPreTokenGenerationV2Handler
{
    public ValueTask<CognitoPreTokenGenerationV2Event> HandleAsync(CognitoPreTokenGenerationV2Event input, RequestContext context, CancellationToken cancellationToken) => ValueTask.FromResult(input);
}
#else
public sealed class PreTokenGenerationHandler : ICognitoPreTokenGenerationHandler
{
    public ValueTask<CognitoPreTokenGenerationEvent> HandleAsync(CognitoPreTokenGenerationEvent input, RequestContext context, CancellationToken cancellationToken) => ValueTask.FromResult(input);
}
#endif
