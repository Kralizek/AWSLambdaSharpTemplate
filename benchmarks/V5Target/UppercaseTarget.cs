#nullable enable

using System;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;

using BenchmarkWorkloads;

using Kralizek.Lambda;

using Microsoft.Extensions.DependencyInjection;

namespace V5Target;

public sealed class UppercaseTarget : IRequestTarget
{
    private readonly UppercaseFunction _function = new();
    private readonly ILambdaContext _context = new TestLambdaContext
    {
        RemainingTime = TimeSpan.FromMinutes(1)
    };

    public Task<string> InvokeAsync(string input) => _function.FunctionHandlerAsync(input, _context);
}

public sealed class UppercaseFunction : RequestResponseFunction<string, string>
{
    protected override void ConfigureServices(IServiceCollection services, IExecutionEnvironment executionEnvironment) =>
        RegisterHandler<UppercaseHandler>(services);
}

public sealed class UppercaseHandler : IRequestResponseHandler<string, string>
{
    public Task<string> HandleAsync(string? input, ILambdaContext context) =>
        Task.FromResult(UppercaseWorkload.Execute(input!));
}
