using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.TestUtilities;

using Kralizek.Lambda;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NUnit.Framework;

namespace Tests.Lambda;

[TestFixture]
public class EventFunctionTests
{
    private static TestEventFunction CreateSystemUnderTest() => new();

    [Test]
    public void Configure_is_invoked_on_initialization()
    {
        var sut = CreateSystemUnderTest();
        Assert.That(sut.IsConfigureInvoked, Is.True);
    }

    [Test]
    public void ConfigureServices_is_invoked_on_initialization()
    {
        var sut = CreateSystemUnderTest();
        Assert.That(sut.IsConfigureServicesInvoked, Is.True);
    }

    [Test]
    public void ConfigureLogging_is_invoked_on_initialization()
    {
        var sut = CreateSystemUnderTest();
        Assert.That(sut.IsConfigureLoggingInvoked, Is.True);
    }

    [Test]
    public async Task FunctionHandlerAsync_invokes_handler_and_passes_context()
    {
        TrackingHandler.Reset();
        var sut = new TrackingHandlerFunction();
        var lambdaContext = new TestLambdaContext { AwsRequestId = "request-id" };

        await sut.FunctionHandlerAsync("expected-value", lambdaContext);

        Assert.That(TrackingHandler.WasInvoked, Is.True);
        Assert.That(TrackingHandler.ReceivedInput, Is.EqualTo("expected-value"));
        Assert.That(TrackingHandler.ReceivedContext?.AwsRequestId, Is.EqualTo("request-id"));
        Assert.That(TrackingHandler.ReceivedContext?.LambdaContext, Is.SameAs(lambdaContext));
    }

    [Test]
    public void FunctionHandlerAsync_propagates_handler_exception()
    {
        var sut = new FailingHandlerFunction();

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.FunctionHandlerAsync("hello", new TestLambdaContext()));
    }

    public class TestEventFunction : EventFunction<string, NoOpHandler>
    {
        protected override void Configure(IConfigurationBuilder builder) => IsConfigureInvoked = true;

        protected override void ConfigureServices(IServiceCollection services, IExecutionEnvironment executionEnvironment) =>
            IsConfigureServicesInvoked = true;

        protected override void ConfigureLogging(ILoggingBuilder logging, IExecutionEnvironment executionEnvironment) =>
            IsConfigureLoggingInvoked = true;

        public bool IsConfigureInvoked { get; private set; }
        public bool IsConfigureServicesInvoked { get; private set; }
        public bool IsConfigureLoggingInvoked { get; private set; }
    }

    public class TrackingHandlerFunction : EventFunction<string, TrackingHandler> { }

    public class FailingHandlerFunction : EventFunction<string, ThrowingHandler> { }

    public class NoOpHandler : IEventHandler<string>
    {
        public ValueTask HandleAsync(string input, EventContext context, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }

    public class TrackingHandler : IEventHandler<string>
    {
        public static bool WasInvoked { get; private set; }
        public static string? ReceivedInput { get; private set; }
        public static EventContext? ReceivedContext { get; private set; }

        public static void Reset()
        {
            WasInvoked = false;
            ReceivedInput = null;
            ReceivedContext = null;
        }

        public ValueTask HandleAsync(string input, EventContext context, CancellationToken cancellationToken)
        {
            WasInvoked = true;
            ReceivedInput = input;
            ReceivedContext = context;
            return ValueTask.CompletedTask;
        }
    }

    public class ThrowingHandler : IEventHandler<string>
    {
        public ValueTask HandleAsync(string input, EventContext context, CancellationToken cancellationToken) =>
            ValueTask.FromException(new InvalidOperationException("boom"));
    }
}