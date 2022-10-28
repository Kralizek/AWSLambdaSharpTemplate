using System;
using Amazon.Lambda.TestUtilities;
using Kralizek.Lambda;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Tests.Lambda;

[TestFixture]
public class RequestResponseFunctionTests
{
    private RequestResponseFunction CreateSystemUnderTest()
    {
        return new RequestResponseFunction();
    }

    [Test]
    public void Configure_should_be_invoked_on_type_initialization()
    {
        var sut = CreateSystemUnderTest();

        Assert.True(sut.IsConfigureInvoked);
    }

    [Test]
    public void ConfigureServices_should_be_invoked_on_type_initialization()
    {
        var sut = CreateSystemUnderTest();

        Assert.True(sut.IsConfigureServicesInvoked);
    }

    [Test]
    public void ConfigureLogging_should_be_invoked_on_type_initialization()
    {
        var sut = CreateSystemUnderTest();

        Assert.True(sut.IsConfigureLoggingInvoked);
    }

    [Test]
    public void FunctionHandlerAsync_throws_if_no_handler_is_registered()
    {
        var sut = CreateSystemUnderTest();

        var context = new TestLambdaContext();

        Assert.ThrowsAsync<InvalidOperationException>(() => sut.FunctionHandlerAsync("Hello World", context));
    }

    public class RequestResponseFunction : RequestResponseFunction<string, string>
    {
        protected override void Configure(IConfigurationBuilder builder) => IsConfigureInvoked = true;

        protected override void ConfigureServices(IServiceCollection services, IExecutionEnvironment executionEnvironment) => IsConfigureServicesInvoked = true;

        protected override void ConfigureLogging(ILoggingBuilder loggerFactory, IExecutionEnvironment executionEnvironment) => IsConfigureLoggingInvoked = true;

        public bool IsConfigureInvoked { get; private set; }

        public bool IsConfigureServicesInvoked { get; private set; }

        public bool IsConfigureLoggingInvoked { get; private set; }
    }
}