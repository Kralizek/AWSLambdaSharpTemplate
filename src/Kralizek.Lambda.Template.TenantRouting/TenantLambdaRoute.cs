using System;
using System.Text;

namespace Kralizek.Lambda;

/// <summary>
/// Describes a synchronous invocation of a tenant-isolated Lambda function.
/// </summary>
public sealed record TenantLambdaRoute
{
    public TenantLambdaRoute(
        string tenantId,
        string functionName,
        ReadOnlyMemory<byte> payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(functionName);

        TenantId = tenantId;
        FunctionName = functionName;
        Payload = payload;
    }

    /// <summary>
    /// Gets the tenant identifier supplied to the downstream Lambda invocation.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// Gets the name or ARN of the downstream Lambda function.
    /// </summary>
    public string FunctionName { get; }

    /// <summary>
    /// Gets the payload sent to the downstream Lambda function.
    /// </summary>
    public ReadOnlyMemory<byte> Payload { get; }

    /// <summary>
    /// Gets an optional Lambda version or alias qualifier.
    /// </summary>
    public string? Qualifier { get; init; }

    /// <summary>
    /// Creates a route whose payload is the UTF-8 encoding of the supplied text.
    /// </summary>
    public static TenantLambdaRoute Utf8(
        string tenantId,
        string functionName,
        string payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        return new TenantLambdaRoute(
            tenantId,
            functionName,
            Encoding.UTF8.GetBytes(payload));
    }
}
