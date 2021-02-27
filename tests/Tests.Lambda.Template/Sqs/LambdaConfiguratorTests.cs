using Amazon.Lambda.SNSEvents;
using Amazon.Lambda.SQSEvents;
using AutoFixture.Idioms;
using Kralizek.Lambda;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Tests.Lambda.Sns;

namespace Tests.Lambda.Sqs
{
    [TestFixture]
    public class LambdaConfiguratorTests
    {
        [Test, CustomAutoData]
        public void Constructor_is_guarded(GuardClauseAssertion assertion) => assertion.Verify(typeof(SqsLambdaConfigurator<TestMessage, TestMessageHandler>).GetConstructors());

        [Test, CustomAutoData]
        public void Properties_are_configured_from_constructor_parameters(WritablePropertyAssertion assertion) => assertion.Verify(typeof(SqsLambdaConfigurator<TestMessage, TestMessageHandler>));
    }

    [TestFixture]
    public class LambdaConfiguratorTestsExtensions
    {
        [Test]
        [CustomInlineAutoData(null)]
        [CustomInlineAutoData]
        public void UseParallelExecution_registers_ParallelSqsEventHandler(int? maxDegreeOfParallelism, SqsLambdaConfigurator<TestMessage, TestMessageHandler> configurator)
        {
            configurator.Services.AddLogging().AddOptions();

            configurator.Services.AddTransient<ISerializer, SystemTextJsonSerializer>();

            configurator.UseParallelExecution(maxDegreeOfParallelism);

            var sp = configurator.Services.BuildServiceProvider();

            var handler = sp.GetService<IEventHandler<SQSEvent>>();

            Assert.That(handler, Is.InstanceOf<ParallelSqsEventHandler<TestMessage>>());
        }

        [Test]
        [CustomInlineAutoData(null)]
        [CustomInlineAutoData]
        public void UseParallelExecution_overrides_default_registration(int? maxDegreeOfParallelism, SqsLambdaConfigurator<TestMessage, TestMessageHandler> configurator)
        {
            configurator.Services.AddLogging().AddOptions();

            configurator.Services.AddTransient<ISerializer, SystemTextJsonSerializer>();

            configurator.Services.AddTransient<IEventHandler<SQSEvent>, SqsEventHandler<TestMessage>>();

            configurator.UseParallelExecution(maxDegreeOfParallelism);

            var sp = configurator.Services.BuildServiceProvider();

            var handler = sp.GetService<IEventHandler<SQSEvent>>();

            Assert.That(handler, Is.InstanceOf<ParallelSqsEventHandler<TestMessage>>());
        }

        [Test, CustomAutoData]
        public void UseParallelExecution_configures_options(SqsLambdaConfigurator<TestMessage, TestMessageHandler> configurator, int maxDegreeOfParallelism)
        {
            configurator.Services.AddLogging().AddOptions();

            configurator.UseParallelExecution(maxDegreeOfParallelism);

            var sp = configurator.Services.BuildServiceProvider();

            var options = sp.GetService<IOptions<ParallelSqsExecutionOptions>>();

            Assert.That(options?.Value.MaxDegreeOfParallelism, Is.EqualTo(maxDegreeOfParallelism));
        }
    }
}
