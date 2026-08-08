using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.CognitoEvents;
using Kralizek.Lambda;
namespace LambdaFunctionProject;
public sealed class PreSignUpHandler : ICognitoPreSignUpHandler
{
    public ValueTask<CognitoPreSignupEvent> HandleAsync(CognitoPreSignupEvent input, RequestContext context, CancellationToken cancellationToken) => ValueTask.FromResult(input);
}
