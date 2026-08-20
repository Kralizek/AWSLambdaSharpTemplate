using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;

using Kralizek.Lambda;

using NUnit.Framework;

namespace Tests.Lambda;

[TestFixture]
public class DeadlineCancellationTests
{
    [SetUp]
    public void SetUp() => TrackingHandler.Reset();

    [TestCase(false)]
    [TestCase(true)]
    public async Task Request_handler_token_is_generic_and_not_deadline_based(bool minimal)
    {
        var lambdaContext = new TestLambdaContext { RemainingTime = TimeSpan.Zero };

        if (minimal)
        {
            await new MinimalTrackingFunction().FunctionHandlerAsync("input", lambdaContext);
        }
        else
        {
            await new FullTrackingFunction().FunctionHandlerAsync("input", lambdaContext);
        }

        Assert.Multiple(() =>
        {
            Assert.That(TrackingHandler.Invoked, Is.True);
            Assert.That(TrackingHandler.Token, Is.EqualTo(CancellationToken.None));
            Assert.That(TrackingHandler.Token.CanBeCanceled, Is.False);
        });
    }

    [Test]
    public void Deadline_cancellation_source_uses_current_remaining_time()
    {
        var lambdaContext = new TestLambdaContext { RemainingTime = TimeSpan.Zero };
        var context = FunctionContextFactory.CreateRequestContext(lambdaContext);

        using var deadline = context.CreateDeadlineCancellationTokenSource();

        Assert.That(deadline.Token.IsCancellationRequested, Is.True);
    }

    [Test]
    public void Deadline_cancellation_source_is_caller_owned_and_created_per_request()
    {
        var lambdaContext = new TestLambdaContext { RemainingTime = TimeSpan.FromMinutes(1) };
        var context = FunctionContextFactory.CreateRequestContext(lambdaContext);

        using var first = context.CreateDeadlineCancellationTokenSource();
        using var second = context.CreateDeadlineCancellationTokenSource();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Token.CanBeCanceled, Is.True);
            Assert.That(second.Token.CanBeCanceled, Is.True);
        });
    }

    public sealed class FullTrackingFunction : RequestFunction<string, string, TrackingHandler>;

    public sealed class MinimalTrackingFunction : MinimalRequestFunction<string, string, TrackingHandler>;

    public sealed class TrackingHandler : IRequestHandler<string, string>
    {
        public static bool Invoked { get; private set; }
        public static CancellationToken Token { get; private set; }

        public static void Reset()
        {
            Invoked = false;
            Token = default;
        }

        public ValueTask<string> HandleAsync(string input, RequestContext context, CancellationToken cancellationToken)
        {
            Invoked = true;
            Token = cancellationToken;
            return ValueTask.FromResult(input);
        }
    }
}
