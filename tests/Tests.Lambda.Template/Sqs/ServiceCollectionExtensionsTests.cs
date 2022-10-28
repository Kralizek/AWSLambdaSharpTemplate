using System.Collections.Generic;
using System.Text;
using Amazon.Lambda.SQSEvents;
using Kralizek.Lambda;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Tests.Lambda.Sns;

namespace Tests.Lambda.Sqs
{
    [TestFixture]
    public class ServiceCollectionExtensionsTests
    {
        [Test]
        public void UseSqsHandler_registers_default_SqsEventHandler()
        {
            var services = new ServiceCollection();

            services.AddLogging();

            services.UseSqsHandler<TestMessage, TestMessageHandler>();

            var serviceProvider = services.BuildServiceProvider();

            var handler = serviceProvider.GetRequiredService<IEventHandler<SQSEvent>>();

            Assert.That(handler, Is.InstanceOf<SqsEventHandler<TestMessage>>());
        }

        [Test]
        public void UseSqsHandler_registers_ParallelSqsEventHandler_when_parallel_execution_is_enabled()
        {
            var services = new ServiceCollection();

            services.AddLogging();

            services.UseSqsHandler<TestMessage, TestMessageHandler>(enableParallelExecution: true);

            var serviceProvider = services.BuildServiceProvider();

            var handler = serviceProvider.GetRequiredService<IEventHandler<SQSEvent>>();

            Assert.That(handler, Is.InstanceOf<ParallelSqsEventHandler<TestMessage>>());
        }

        [Test]
        public void UseSqsHandler_registers_IMessageHandler()
        {
            var services = new ServiceCollection();

            services.AddLogging();

            services.UseSqsHandler<TestMessage, TestMessageHandler>();

            var serviceProvider = services.BuildServiceProvider();

            serviceProvider.GetRequiredService<IMessageHandler<TestMessage>>();
        }

        [Test]
        public void UseSqsHandler_registers_IMessageSerializer()
        {
            var services = new ServiceCollection();

            services.AddLogging();

            services.UseSqsHandler<TestMessage, TestMessageHandler>();

            var serviceProvider = services.BuildServiceProvider();

            serviceProvider.GetRequiredService<IMessageSerializer>();
        }
        
        [Test]
        public void UseQueueMessageHandler_registers_default_SqsEventHandler()
        {
            var services = new ServiceCollection();

            services.AddLogging();

            services.UseQueueMessageHandler<TestMessage, TestMessageHandler>();

            var serviceProvider = services.BuildServiceProvider();

            var handler = serviceProvider.GetRequiredService<IEventHandler<SQSEvent>>();

            Assert.That(handler, Is.InstanceOf<SqsEventHandler<TestMessage>>());
        }

        [Test]
        public void UseQueueMessageHandler_registers_ParallelSqsEventHandler_when_parallel_execution_is_enabled()
        {
            var services = new ServiceCollection();

            services.AddLogging();

            services.UseQueueMessageHandler<TestMessage, TestMessageHandler>().WithParallelExecution();

            var serviceProvider = services.BuildServiceProvider();

            var handler = serviceProvider.GetRequiredService<IEventHandler<SQSEvent>>();

            Assert.That(handler, Is.InstanceOf<ParallelSqsEventHandler<TestMessage>>());
        }

        [Test]
        public void UseQueueMessageHandler_registers_IMessageHandler()
        {
            var services = new ServiceCollection();

            services.AddLogging();

            services.UseQueueMessageHandler<TestMessage, TestMessageHandler>();

            var serviceProvider = services.BuildServiceProvider();

            serviceProvider.GetRequiredService<IMessageHandler<TestMessage>>();
        }

        [Test]
        public void UseQueueMessageHandler_registers_IMessageSerializer()
        {
            var services = new ServiceCollection();

            services.AddLogging();

            services.UseQueueMessageHandler<TestMessage, TestMessageHandler>();

            var serviceProvider = services.BuildServiceProvider();

            serviceProvider.GetRequiredService<IMessageSerializer>();
        }
    }
}
