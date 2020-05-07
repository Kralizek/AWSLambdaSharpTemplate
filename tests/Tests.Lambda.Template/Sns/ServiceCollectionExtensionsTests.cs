using System.Threading.Tasks;
using Amazon.Lambda.Core;
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
        public void UseNotificationHandler_registers_SnsEventHandler()
        {
            var services = new ServiceCollection();

            services.AddLogging();

            services.UseNotificationHandler<TestNotification, TestNotificationHandler>();

            var serviceProvider = services.BuildServiceProvider();

            serviceProvider.GetRequiredService<IEventHandler<SNSEvent>>();
        }

        [Test]
        public void UseForEachAsyncNotificationHandler_registers_SnsForEachAsyncEventHandler()
        {
            var services = new ServiceCollection();

            services.AddLogging();

            services.UseForEachAsyncSnsHandler<TestNotification, TestNotificationHandler>();

            var serviceProvider = services.BuildServiceProvider();

            serviceProvider.GetRequiredService<IEventHandler<SNSEvent>>();
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
        public void UseNotificationHandler_With_ForEachSqsHandlerregisters_INotificationHandler()
        {
            var services = new ServiceCollection();

            services.AddLogging();

            services.UseForEachAsyncSnsHandler<TestNotification, TestNotificationHandler>();

            var serviceProvider = services.BuildServiceProvider();

            serviceProvider.GetRequiredService<INotificationHandler<TestNotification>>();
        }
    }
}