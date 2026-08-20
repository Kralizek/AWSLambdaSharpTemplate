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
    public void SetUp()
    {
        TrackingHandler.Reset();
        DeadlineTrackingHandler.Reset();
    }

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

    [TestCase(false)]
    [TestCase(true)]
    public async Task Deadline_token_is_created_lazily_from_remaining_time_and_cached(bool minimal)
    {
        var lambdaContext = new TestLambdaContext { RemainingTime = TimeSpan.Zero };

        if (minimal)
        {
            await new MinimalDeadlineTrackingFunction().FunctionHandlerAsync("input", lambdaContext);
        }
        else
        {
            await new FullDeadlineTrackingFunction().FunctionHandlerAsync("input", lambdaContext);
        }

        Assert.Multiple(() =>
        {
            Assert.That(DeadlineTrackingHandler.FirstToken.IsCancellationRequested, Is.True);
            Assert.That(DeadlineTrackingHandler.SecondToken, Is.EqualTo(DeadlineTrackingHandler.FirstToken));
        });
    }

    public sealed class FullTrackingFunction : RequestFunction<string, string, TrackingHandler>;

    public sealed class MinimalTrackingFunction : MinimalRequestFunction<string, string, TrackingHandler>;

    public sealed class FullDeadlineTrackingFunction : RequestFunction<string, string, DeadlineTrackingHandler>;

    public sealed class MinimalDeadlineTrackingFunction : MinimalRequestFunction<string, string, DeadlineTrackingHandler>;

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

    public sealed class DeadlineTrackingHandler : IRequestHandler<string, string>
    {
        public static CancellationToken FirstToken { get; private set; }
        public static CancellationToken SecondToken { get; private set; }

        public static void Reset()
        {
            FirstToken = default;
            SecondToken = default;
        }

        public ValueTask<string> HandleAsync(string input, RequestContext context, CancellationToken cancellationToken)
        {
            FirstToken = context.GetDeadlineCancellationToken();
            SecondToken = context.GetDeadlineCancellationToken();
            return ValueTask.FromResult(input);
        }
    }
}
