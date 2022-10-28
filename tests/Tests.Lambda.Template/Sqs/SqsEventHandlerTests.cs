using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.TestUtilities;
using Kralizek.Lambda;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Tests.Lambda.Sqs {
    [TestFixture]
    public class SqsEventHandlerTests
    {
        private Mock<IMessageSerializer> mockMessageSerializer;
        private Mock<IMessageHandler<TestMessage>> mockMessageHandler;
        private Mock<IServiceScopeFactory> mockServiceScopeFactory;
        private Mock<IServiceProvider> mockServiceProvider;
        private Mock<ILoggerFactory> mockLoggerFactory;
        private Mock<IServiceScope> mockServiceScope;


        [SetUp]
        public void Initialize()
        {
            mockMessageSerializer = new Mock<IMessageSerializer>();

            mockMessageSerializer
                .Setup(p => p.Deserialize<TestMessage>(It.IsAny<string>()))
                .Returns(() => new TestMessage());
            
            mockMessageHandler = new Mock<IMessageHandler<TestMessage>>();
            mockMessageHandler.Setup(p => p.HandleAsync(It.IsAny<TestMessage>(), It.IsAny<ILambdaContext>())).Returns(Task.CompletedTask);

            mockServiceScope = new Mock<IServiceScope>();

            mockServiceScopeFactory = new Mock<IServiceScopeFactory>();

            mockServiceScopeFactory.Setup(p => p.CreateScope()).Returns(mockServiceScope.Object);

            mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider.Setup(p => p.GetService(typeof(IMessageHandler<TestMessage>)))
                               .Returns(mockMessageHandler.Object);
            mockServiceProvider.Setup(p => p.GetService(typeof(IServiceScopeFactory)))
                               .Returns(mockServiceScopeFactory.Object);

            mockServiceProvider
                .Setup(p => p.GetService(typeof(IMessageSerializer)))
                .Returns(mockMessageSerializer.Object);

            mockServiceScope.Setup(p => p.ServiceProvider).Returns(mockServiceProvider.Object);

            mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(p => p.CreateLogger(It.IsAny<string>()))
                             .Returns(Mock.Of<ILogger>());
        }

        private SqsEventHandler<TestMessage> CreateSystemUnderTest()
        {
            return new SqsEventHandler<TestMessage>(mockServiceProvider.Object, mockLoggerFactory.Object);
        }

        [Test]
        public async Task HandleAsync_resolves_MessageHandler_for_each_record()
        {
            var sqsEvent = new SQSEvent
            {
                Records = new List<SQSEvent.SQSMessage>
                {
                    new SQSEvent.SQSMessage
                    {
                        Body = "{}"
                    },
                    new SQSEvent.SQSMessage
                    {
                        Body = "{}"
                    },
                }
            };

            var lambdaContext = new TestLambdaContext();

            var sut = CreateSystemUnderTest();

            await sut.HandleAsync(sqsEvent, lambdaContext);

            mockServiceProvider.Verify(p => p.GetService(typeof(IMessageHandler<TestMessage>)), Times.Exactly(sqsEvent.Records.Count));
        }

        [Test]
        public async Task HandleAsync_creates_a_scope_for_each_record()
        {
            var sqsEvent = new SQSEvent
            {
                Records = new List<SQSEvent.SQSMessage>
                {
                    new SQSEvent.SQSMessage
                    {
                        Body = "{}"
                    },
                    new SQSEvent.SQSMessage
                    {
                        Body = "{}"
                    },
                }
            };

            var lambdaContext = new TestLambdaContext();

            var sut = CreateSystemUnderTest();

            await sut.HandleAsync(sqsEvent, lambdaContext);

            mockServiceScopeFactory.Verify(p => p.CreateScope(), Times.Exactly(sqsEvent.Records.Count));
        }

        [Test]
        public async Task HandleAsync_executes_NotificationHandler_for_each_record()
        {
            var sqsEvent = new SQSEvent
            {
                Records = new List<SQSEvent.SQSMessage>
                {
                    new SQSEvent.SQSMessage
                    {
                        Body = "{}"
                    },
                    new SQSEvent.SQSMessage
                    {
                        Body = "{}"
                    },
                }
            };

            var lambdaContext = new TestLambdaContext();

            var sut = CreateSystemUnderTest();

            await sut.HandleAsync(sqsEvent, lambdaContext);

            mockMessageHandler.Verify(p => p.HandleAsync(It.IsAny<TestMessage>(), lambdaContext), Times.Exactly(sqsEvent.Records.Count));
        }

        [Test]
        public void HandleAsync_throws_InvalidOperation_if_NotificationHandler_is_not_registered()
        {
            var sqsEvent = new SQSEvent
            {
                Records = new List<SQSEvent.SQSMessage>
                {
                    new SQSEvent.SQSMessage
                    {
                        Body = "{}"
                    },
                    new SQSEvent.SQSMessage
                    {
                        Body = "{}"
                    },
                }
            };

            var lambdaContext = new TestLambdaContext();

            mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider.Setup(p => p.GetService(typeof(IServiceScopeFactory))).Returns(mockServiceScopeFactory.Object);
            
            mockServiceScope.Setup(p => p.ServiceProvider).Returns(mockServiceProvider.Object);

            var sut = CreateSystemUnderTest();

            Assert.ThrowsAsync<InvalidOperationException>(() => sut.HandleAsync(sqsEvent, lambdaContext));
        }
    }
}