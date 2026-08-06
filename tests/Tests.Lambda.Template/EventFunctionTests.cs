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
    public async Task FunctionHandlerAsync_invokes_handler()
    {
        TrackingHandler.Reset();
        var sut = new TrackingHandlerFunction();

        await sut.FunctionHandlerAsync("hello", new TestLambdaContext());

        Assert.That(TrackingHandler.WasInvoked, Is.True);
    }

    [Test]
    public async Task FunctionHandlerAsync_passes_input_to_handler()
    {
        TrackingHandler.Reset();
        var sut = new TrackingHandlerFunction();

        await sut.FunctionHandlerAsync("expected-value", new TestLambdaContext());

        Assert.That(TrackingHandler.ReceivedInput, Is.EqualTo("expected-value"));
    }

    [Test]
    public void FunctionHandlerAsync_propagates_handler_exception()
    {
        var sut = new FailingHandlerFunction();

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.FunctionHandlerAsync("hello", new TestLambdaContext()));
    }

    // --- test function classes ---

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

    // --- handler classes ---

    public class NoOpHandler : IEventHandler<string>
    {
        public ValueTask HandleAsync(string input, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }

    public class TrackingHandler : IEventHandler<string>
    {
        public static bool WasInvoked { get; private set; }
        public static string? ReceivedInput { get; private set; }

        public static void Reset()
        {
            WasInvoked = false;
            ReceivedInput = null;
        }

        public ValueTask HandleAsync(string input, CancellationToken cancellationToken)
        {
            WasInvoked = true;
            ReceivedInput = input;
            return ValueTask.CompletedTask;
        }
    }

    public class ThrowingHandler : IEventHandler<string>
    {
        public ValueTask HandleAsync(string input, CancellationToken cancellationToken) =>
            ValueTask.FromException(new InvalidOperationException("boom"));
    }
}