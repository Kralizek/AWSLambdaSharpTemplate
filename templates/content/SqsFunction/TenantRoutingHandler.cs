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
        var tenantId = ResolveTenantId(record);
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return SqsRecordResult.Failed("Unable to resolve tenant ID.");
        }

        await router.RouteAsync(
            TenantLambdaRoute.Utf8(
                tenantId,
                "TargetFunction",
                record.Body ?? string.Empty),
            cancellationToken).ConfigureAwait(false);

        return SqsRecordResult.Success;
    }

    private static string? ResolveTenantId(SQSEvent.SQSMessage record)
    {
        // This is only an example. Resolve the tenant from message attributes,
        // MessageGroupId, the payload, a registry, or any application-specific source.
        return record.MessageAttributes is not null
            && record.MessageAttributes.TryGetValue("tenant-id", out var tenantAttribute)
                ? tenantAttribute.StringValue
                : null;
    }
}
