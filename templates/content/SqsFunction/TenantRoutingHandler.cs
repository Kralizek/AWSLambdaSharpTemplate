using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.SQSEvents;

using Kralizek.Lambda;

namespace LambdaFunctionProject;

public sealed class TenantRoutingHandler(
    ITenantLambdaRouter router)
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

        await router.RouteAsync(
            TenantLambdaRoute.Utf8(
                tenantAttribute.StringValue,
                "TargetFunction",
                record.Body ?? string.Empty),
            cancellationToken).ConfigureAwait(false);

        return SqsRecordResult.Success;
    }
}
