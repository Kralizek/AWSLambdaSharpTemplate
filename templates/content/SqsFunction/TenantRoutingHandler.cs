using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.SQSEvents;

using Kralizek.Lambda;

using Microsoft.Extensions.Configuration;

namespace LambdaFunctionProject;

public sealed class TenantRoutingHandler(
    ITenantLambdaRouter router,
    IConfiguration configuration)
    : ISqsRecordHandler
{
    public async ValueTask<SqsRecordResult> HandleAsync(
        SQSEvent.SQSMessage record,
        SqsMessageContext context,
        CancellationToken cancellationToken)
    {
        if (record.MessageAttributes is null
            || !record.MessageAttributes.TryGetValue("tenant-id", out var tenantAttribute)
            || string.IsNullOrWhiteSpace(tenantAttribute.StringValue))
        {
            return SqsRecordResult.Failed("Missing tenant-id message attribute.");
        }

        var targetFunction = configuration["TenantRouting:FunctionName"];
        if (string.IsNullOrWhiteSpace(targetFunction))
        {
            throw new InvalidOperationException(
                "Configure TenantRouting:FunctionName with the downstream tenant-isolated Lambda function name or ARN.");
        }

        await router.RouteAsync(
            TenantLambdaRoute.Utf8(
                tenantAttribute.StringValue,
                targetFunction,
                record.Body ?? string.Empty),
            cancellationToken).ConfigureAwait(false);

        return SqsRecordResult.Success;
    }
}
