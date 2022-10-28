using Amazon.Lambda.SNSEvents;
using Kralizek.Lambda;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Tests.Lambda.Sns
{
    [TestFixture]
    public class ServiceCollectionExtensionsTests
    {
        [Test]
        public void UseNotificationHandler_registers_default_SnsEventHandler()
        {
            var services = new ServiceCollection();

            services.AddLogging();

            services.UseNotificationHandler<TestNotification, TestNotificationHandler>();

            var serviceProvider = services.BuildServiceProvider();

            var handler = serviceProvider.GetRequiredService<IEventHandler<SNSEvent>>();

            Assert.That(handler, Is.InstanceOf<SnsEventHandler<TestNotification>>());
        }

        [Test]
        public void UseNotificationHandler_registers_ParallelSnsEventHandler_when_parallel_execution_is_enabled()
        {
            var services = new ServiceCollection();

            services.AddLogging();

            services.UseNotificationHandler<TestNotification, TestNotificationHandler>(enableParallelExecution: true);

            var serviceProvider = services.BuildServiceProvider();

            var handler = serviceProvider.GetRequiredService<IEventHandler<SNSEvent>>();

            Assert.That(handler, Is.InstanceOf<ParallelSnsEventHandler<TestNotification>>());
        }

        [Test]
        public void UseNotificationHandler_registers_INotificationHandler()
        {
            var services = new ServiceCollection();

            services.AddLogging();

            services.UseNotificationHandler<TestNotification, TestNotificationHandler>();

            var serviceProvider = services.BuildServiceProvider();

            serviceProvider.GetRequiredService<INotificationHandler<TestNotification>>();
        }
        
        [Test]
        public void UseNotificationHandler_registers_INotificationSerializer()
        {
            var services = new ServiceCollection();

            services.AddLogging();

            services.UseNotificationHandler<TestNotification, TestNotificationHandler>();

            var serviceProvider = services.BuildServiceProvider();

            serviceProvider.GetRequiredService<INotificationSerializer>();
        }
    }
}