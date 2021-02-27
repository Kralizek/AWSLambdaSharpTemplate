using Amazon.Lambda.SNSEvents;
using AutoFixture.Idioms;
using Kralizek.Lambda;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Tests.Lambda.Sqs;

namespace Tests.Lambda.Sns
{
    [TestFixture]
    public class LambdaConfiguratorTests
    {
        [Test, CustomAutoData]
        public void Constructor_is_guarded(GuardClauseAssertion assertion) => assertion.Verify(typeof(SnsLambdaConfigurator<TestNotification, TestNotificationHandler>).GetConstructors());

        [Test, CustomAutoData]
        public void Properties_are_configured_from_constructor_parameters(WritablePropertyAssertion assertion) => assertion.Verify(typeof(SnsLambdaConfigurator<TestNotification, TestNotificationHandler>));
    }

    [TestFixture]
    public class LambdaConfiguratorExtensionsTests
    {
        [Test]
        [CustomInlineAutoData(null)]
        [CustomInlineAutoData]
        public void UseParallelExecution_registers_ParallelSnsEventHandler(int? maxDegreeOfParallelism, SnsLambdaConfigurator<TestNotification, TestNotificationHandler> configurator)
        {
            configurator.Services.AddLogging();

            configurator.Services.AddTransient<ISerializer, SystemTextJsonSerializer>();

            configurator.UseParallelExecution(maxDegreeOfParallelism);

            var sp = configurator.Services.BuildServiceProvider();

            var handler = sp.GetService<IEventHandler<SNSEvent>>();

            Assert.That(handler, Is.InstanceOf<ParallelSnsEventHandler<TestNotification>>());
        }

        [Test]
        [CustomInlineAutoData(null)]
        [CustomInlineAutoData]
        public void UseParallelExecution_overrides_default_registration(int? maxDegreeOfParallelism, SnsLambdaConfigurator<TestNotification, TestNotificationHandler> configurator)
        {
            configurator.Services.AddLogging();

            configurator.Services.AddTransient<ISerializer, SystemTextJsonSerializer>();

            configurator.Services.AddTransient<IEventHandler<SNSEvent>, SnsEventHandler<TestNotification>>();

            configurator.UseParallelExecution(maxDegreeOfParallelism);

            var sp = configurator.Services.BuildServiceProvider();

            var handler = sp.GetService<IEventHandler<SNSEvent>>();

            Assert.That(handler, Is.InstanceOf<ParallelSnsEventHandler<TestNotification>>());
        }

        [Test, CustomAutoData]
        public void UseParallelExecution_configures_options(SnsLambdaConfigurator<TestNotification, TestNotificationHandler> configurator, int maxDegreeOfParallelism)
        {
            configurator.Services.AddLogging();

            configurator.UseParallelExecution(maxDegreeOfParallelism);

            var sp = configurator.Services.BuildServiceProvider();

            var options = sp.GetService<IOptions<ParallelSnsExecutionOptions>>();

            Assert.That(options?.Value.MaxDegreeOfParallelism, Is.EqualTo(maxDegreeOfParallelism));
        }
    }
}
