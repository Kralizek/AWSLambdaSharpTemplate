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
        public void UseSqsHandler_registers_UseSqsHandler()
        {
            var services = new ServiceCollection();

            services.AddLogging();

            services.UseSqsHandler<TestMessage, TestMessageHandler>();

            var serviceProvider = services.BuildServiceProvider();

            serviceProvider.GetRequiredService<IEventHandler<SQSEvent>>();
        }

        [Test]
        public void UseNotificationHandler_registers_INotificationHandler()
        {
            var services = new ServiceCollection();

            services.AddLogging();

            services.UseSqsHandler<TestMessage, TestMessageHandler>();

            var serviceProvider = services.BuildServiceProvider();

            serviceProvider.GetRequiredService<IMessageHandler<TestMessage>>();
        }
    }
}
