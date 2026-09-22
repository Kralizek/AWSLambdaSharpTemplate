using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda;
using Amazon.Lambda.Model;

namespace Kralizek.Lambda;

internal sealed class TenantLambdaRouter(IAmazonLambda lambda) : ITenantLambdaRouter
{
    public async ValueTask RouteAsync(
        TenantLambdaRoute route,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(route);

        using var activity = LambdaTelemetry.ActivitySource.StartActivity(
            route.FunctionName,
            ActivityKind.Client);

        activity?.SetTag("faas.invoked_name", route.FunctionName);
        activity?.SetTag("faas.invoked_provider", "aws");
        activity?.SetTag("kralizek.aws.lambda.tenant_id", route.TenantId);

        using var payload = new MemoryStream(route.Payload.ToArray(), writable: false);

        var request = new InvokeRequest
        {
            FunctionName = route.FunctionName,
            InvocationType = InvocationType.RequestResponse,
            PayloadStream = payload,
            Qualifier = route.Qualifier,
            TenantId = route.TenantId
        };

        var response = await lambda
            .InvokeAsync(request, cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrEmpty(response.FunctionError))
        {
            return;
        }

        activity?.SetStatus(ActivityStatusCode.Error, response.FunctionError);

        throw new TenantLambdaInvocationException(
            route.FunctionName,
            route.TenantId,
            response.FunctionError);
    }
}
