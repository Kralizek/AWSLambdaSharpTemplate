using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.CognitoEvents;
using Kralizek.Lambda;
namespace LambdaFunctionProject;
public sealed class PostConfirmationHandler : ICognitoPostConfirmationHandler
{
    public ValueTask<CognitoPostConfirmationEvent> HandleAsync(CognitoPostConfirmationEvent input, RequestContext context, CancellationToken cancellationToken) => ValueTask.FromResult(input);
}
