using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.CognitoEvents;
using Kralizek.Lambda;
namespace LambdaFunctionProject;
public sealed class CustomEmailSenderHandler : ICognitoCustomEmailSenderHandler
{
    public ValueTask<CognitoCustomEmailSenderEvent> HandleAsync(CognitoCustomEmailSenderEvent input, RequestContext context, CancellationToken cancellationToken) => ValueTask.FromResult(input);
}
