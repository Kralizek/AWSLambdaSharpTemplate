using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.CognitoEvents;
using Kralizek.Lambda;
namespace LambdaFunctionProject;
public sealed class UserMigrationHandler : ICognitoUserMigrationHandler
{
    public ValueTask<CognitoMigrateUserEvent> HandleAsync(CognitoMigrateUserEvent input, RequestContext context, CancellationToken cancellationToken) => ValueTask.FromResult(input);
}
