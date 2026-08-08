using System;

using Amazon.Lambda.TestUtilities;

namespace Tests.Lambda;

internal static class TestLambdaContexts
{
    public static TestLambdaContext Create() => new()
    {
        RemainingTime = TimeSpan.FromMinutes(1)
    };
}