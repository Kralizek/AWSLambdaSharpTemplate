using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.CognitoEvents;
using Amazon.Lambda.Core;

using Kralizek.Lambda;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace CognitoPreSignUpFunction;

public sealed class Function : CognitoPreSignUpFunction<PreSignUpHandler>;

public sealed class PreSignUpHandler : ICognitoPreSignUpHandler
{
    public ValueTask<CognitoPreSignupEvent> HandleAsync(
        CognitoPreSignupEvent input,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        input.Response ??= new CognitoPreSignupResponse();
        input.Response.AutoConfirmUser = true;
        return ValueTask.FromResult(input);
    }
}