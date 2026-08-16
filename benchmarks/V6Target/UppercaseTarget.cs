using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;

using BenchmarkWorkloads;

using Kralizek.Lambda;

namespace V6Target;

public sealed class UppercaseTarget : IRequestTarget
{
    private readonly UppercaseFunction _function = new();
    private readonly ILambdaContext _context = new TestLambdaContext
    {
        RemainingTime = TimeSpan.FromMinutes(1)
    };

    public Task<string> InvokeAsync(string input) => _function.FunctionHandlerAsync(input, _context);
}

public sealed class UppercaseFunction : RequestFunction<string, string, UppercaseHandler>;

public sealed class UppercaseHandler : IRequestHandler<string, string>
{
    public ValueTask<string> HandleAsync(string input, RequestContext context, CancellationToken cancellationToken) =>
        new(UppercaseWorkload.Execute(input));
}
