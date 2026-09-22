using System;

namespace Kralizek.Lambda;

/// <summary>
/// Represents a downstream Lambda invocation that reached the function but completed with a function error.
/// </summary>
public sealed class TenantLambdaInvocationException : Exception
{
    public TenantLambdaInvocationException(
        string functionName,
        string tenantId,
        string functionError)
        : base($"Tenant-isolated Lambda function '{functionName}' failed with '{functionError}'.")
    {
        FunctionName = functionName;
        TenantId = tenantId;
        FunctionError = functionError;
    }

    public string FunctionName { get; }

    public string TenantId { get; }

    public string FunctionError { get; }
}
