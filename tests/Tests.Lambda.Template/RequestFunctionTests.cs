using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;

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
    public void ConfigureConfiguration_is_invoked_on_initialization()
    {
        var sut = CreateSystemUnderTest();
        Assert.That(sut.IsConfigureConfigurationInvoked, Is.True);
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
        var lambdaContext = TestLambdaContexts.Create();
        lambdaContext.AwsRequestId = "request-id";

        var result = await sut.FunctionHandlerAsync("hello", lambdaContext);

        Assert.That(result, Is.EqualTo("hello-result"));
        Assert.That(EchoHandler.ReceivedContext?.AwsRequestId, Is.EqualTo("request-id"));
        Assert.That(EchoHandler.ReceivedContext?.LambdaContext, Is.SameAs(lambdaContext));
    }

    [Test]
    public async Task FunctionHandlerAsync_passes_specialized_context_without_casts()
    {
        SpecializedRequestHandler.ReceivedContext = null;
        var sut = new SpecializedRequestFunction();
        var lambdaContext = TestLambdaContexts.Create();

        var result = await sut.FunctionHandlerAsync("hello", lambdaContext);

        Assert.That(result, Is.EqualTo("hello:context"));
        Assert.That(SpecializedRequestHandler.ReceivedContext, Is.Not.Null);
        Assert.That(SpecializedRequestHandler.ReceivedContext?.Input, Is.EqualTo("hello"));
        Assert.That(SpecializedRequestHandler.ReceivedContext?.LambdaContext, Is.SameAs(lambdaContext));
    }

    [Test]
    public void FunctionHandlerAsync_propagates_handler_exception()
    {
        var sut = new FailingHandlerFunction();

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.FunctionHandlerAsync("hello", TestLambdaContexts.Create()));
    }

    public class TestRequestFunction : RequestFunction<string, string, NoOpHandler>
    {
        protected override void ConfigureConfiguration(IConfigurationBuilder configuration) => IsConfigureConfigurationInvoked = true;

        protected override void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
            IsConfigureServicesInvoked = true;

        protected override void ConfigureLogging(ILoggingBuilder logging) =>
            IsConfigureLoggingInvoked = true;

        public bool IsConfigureConfigurationInvoked { get; private set; }
        public bool IsConfigureServicesInvoked { get; private set; }
        public bool IsConfigureLoggingInvoked { get; private set; }
    }

    public class EchoHandlerFunction : RequestFunction<string, string, EchoHandler> { }

    public class FailingHandlerFunction : RequestFunction<string, string, ThrowingHandler> { }

    public class SpecializedRequestFunction : RequestFunction<string, string, SpecializedRequestContext, SpecializedRequestHandler>
    {
        protected override SpecializedRequestContext CreateContext(string input, ILambdaContext context) => new(context, input);
    }

    public class SpecializedRequestContext : RequestContext
    {
        public SpecializedRequestContext(ILambdaContext context, string input)
            : base(context)
        {
            Input = input;
        }

        public string Input { get; }
    }

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

    public class SpecializedRequestHandler : IRequestHandler<string, string, SpecializedRequestContext>
    {
        public static SpecializedRequestContext? ReceivedContext { get; set; }

        public ValueTask<string> HandleAsync(string input, SpecializedRequestContext context, CancellationToken cancellationToken)
        {
            ReceivedContext = context;
            return ValueTask.FromResult($"{input}:context");
        }
    }

    public class ThrowingHandler : IRequestHandler<string, string>
    {
        public ValueTask<string> HandleAsync(string input, RequestContext context, CancellationToken cancellationToken) =>
            ValueTask.FromException<string>(new InvalidOperationException("boom"));
    }
}