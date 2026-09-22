using System;

using Amazon.Lambda.TestUtilities;

namespace Tests.Lambda;

internal static class TestLambdaContexts
{
    public static TestLambdaContext Create(string? tenantId = null) => new()
    {
        RemainingTime = TimeSpan.FromMinutes(1),
        TenantId = tenantId ?? string.Empty
    };
}