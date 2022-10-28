using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using Kralizek.Lambda;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Tests.Lambda;

[TestFixture]
public class EventFunctionTests
{
    private TestEventFunction CreateSystemUnderTest()
    {
        return new TestEventFunction();
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

    public class TestEventFunction : EventFunction<string>
    {
        protected override void Configure(IConfigurationBuilder builder) => IsConfigureInvoked = true;

        protected override void ConfigureServices(IServiceCollection services, IExecutionEnvironment executionEnvironment) => IsConfigureServicesInvoked = true;

        protected override void ConfigureLogging(ILoggingBuilder loggerFactory, IExecutionEnvironment executionEnvironment) => IsConfigureLoggingInvoked = true;

        public bool IsConfigureInvoked { get; private set; }

        public bool IsConfigureServicesInvoked { get; private set; }

        public bool IsConfigureLoggingInvoked { get; private set; }
    }
}