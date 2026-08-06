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
    public async Task FunctionHandlerAsync_returns_handler_result_and_passes_context()
    {
        EchoHandler.Reset();
        EchoHandler.Suffix = "-result";
        var sut = new EchoHandlerFunction();
        var lambdaContext = new TestLambdaContext { AwsRequestId = "request-id" };

        var result = await sut.FunctionHandlerAsync("hello", lambdaContext);

        Assert.That(result, Is.EqualTo("hello-result"));
        Assert.That(EchoHandler.ReceivedContext?.AwsRequestId, Is.EqualTo("request-id"));
        Assert.That(EchoHandler.ReceivedContext?.LambdaContext, Is.SameAs(lambdaContext));
    }

    [Test]
    public void FunctionHandlerAsync_propagates_handler_exception()
    {
        var sut = new FailingHandlerFunction();

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.FunctionHandlerAsync("hello", new TestLambdaContext()));
    }

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

    public class NoOpHandler : IRequestHandler<string, string>
    {
        public ValueTask<string> HandleAsync(string input, RequestContext context, CancellationToken cancellationToken) => new(input);
    }

    public class EchoHandler : IRequestHandler<string, string>
    {
        public static string Suffix { get; set; } = string.Empty;
        public static RequestContext? ReceivedContext { get; private set; }

        public static void Reset()
        {
            Suffix = string.Empty;
            ReceivedContext = null;
        }

        public ValueTask<string> HandleAsync(string input, RequestContext context, CancellationToken cancellationToken)
        {
            ReceivedContext = context;
            return new ValueTask<string>(input + Suffix);
        }
    }

    public class ThrowingHandler : IRequestHandler<string, string>
    {
        public ValueTask<string> HandleAsync(string input, RequestContext context, CancellationToken cancellationToken) =>
            ValueTask.FromException<string>(new InvalidOperationException("boom"));
    }
}