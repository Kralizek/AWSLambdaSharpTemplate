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
public class RequestFunctionTests
{
    private static TestRequestFunction CreateSystemUnderTest() => new();

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
    public async Task FunctionHandlerAsync_returns_handler_result()
    {
        EchoHandler.Suffix = "-result";
        var sut = new EchoHandlerFunction();

        var result = await sut.FunctionHandlerAsync("hello", new TestLambdaContext());

        Assert.That(result, Is.EqualTo("hello-result"));
    }

    [Test]
    public async Task FunctionHandlerAsync_passes_input_to_handler()
    {
        EchoHandler.Suffix = string.Empty;
        var sut = new EchoHandlerFunction();

        var result = await sut.FunctionHandlerAsync("expected-value", new TestLambdaContext());

        Assert.That(result, Is.EqualTo("expected-value"));
    }

    [Test]
    public void FunctionHandlerAsync_propagates_handler_exception()
    {
        var sut = new FailingHandlerFunction();

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.FunctionHandlerAsync("hello", new TestLambdaContext()));
    }

    // --- test function classes ---

    public class TestRequestFunction : RequestFunction<string, string, NoOpHandler>
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

    public class EchoHandlerFunction : RequestFunction<string, string, EchoHandler> { }

    public class FailingHandlerFunction : RequestFunction<string, string, ThrowingHandler> { }

    // --- handler classes ---

    public class NoOpHandler : IRequestHandler<string, string>
    {
        public ValueTask<string> HandleAsync(string input, CancellationToken cancellationToken) => new(input);
    }

    public class EchoHandler : IRequestHandler<string, string>
    {
        public static string Suffix { get; set; } = string.Empty;

        public ValueTask<string> HandleAsync(string input, CancellationToken cancellationToken) =>
            new(input + Suffix);
    }

    public class ThrowingHandler : IRequestHandler<string, string>
    {
        public ValueTask<string> HandleAsync(string input, CancellationToken cancellationToken) =>
            ValueTask.FromException<string>(new InvalidOperationException("boom"));
    }
}