using System;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;

using BenchmarkWorkloads;

namespace RawSdkTarget;

public sealed class UppercaseTarget : IRequestTarget
{
    private readonly UppercaseFunction _function = new();
    private readonly ILambdaContext _context = new TestLambdaContext
    {
        RemainingTime = TimeSpan.FromMinutes(1)
    };

    public Task<string> InvokeAsync(string input) => _function.FunctionHandlerAsync(input, _context);
}

public sealed class UppercaseFunction
{
    public Task<string> FunctionHandlerAsync(string input, ILambdaContext context) =>
        Task.FromResult(UppercaseWorkload.Execute(input));
}
