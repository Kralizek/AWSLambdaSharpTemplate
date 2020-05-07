using System;
using System.Collections.Concurrent;
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

namespace Tests.Lambda.Sqs
{
    [TestFixture]
    public class SqsForEachAsyncEventHandlerTests
    {
        private Mock<IMessageHandler<TestMessage>> mockMessageHandler;
        private Mock<IServiceScopeFactory> mockServiceScopeFactory;
        private Mock<IServiceProvider> mockServiceProvider;
        private Mock<ILoggerFactory> mockLoggerFactory;
        private Mock<IServiceScope> mockServiceScope;
        private ForEachAsyncHandlingOption forEachAsyncHandlingOption;

        [SetUp]
        public void Initialize()
        {
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

            mockServiceScope.Setup(p => p.ServiceProvider).Returns(mockServiceProvider.Object);

            mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(p => p.CreateLogger(It.IsAny<string>()))
                .Returns(Mock.Of<ILogger>());

            forEachAsyncHandlingOption = new ForEachAsyncHandlingOption { MaxDegreeOfParallelism = 4 };
        }

        private SqsForEachAsyncEventHandler<TestMessage> CreateSystemUnderTest()
        {
            return new SqsForEachAsyncEventHandler<TestMessage>(mockServiceProvider.Object, mockLoggerFactory.Object, forEachAsyncHandlingOption);
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

        [Test]
        public async Task MaxDegreeOfParallelism_Should_ProperlyPropagated()
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
                    new SQSEvent.SQSMessage
                    {
                        Body = "{}"
                    },
                }
            };

            var cq = new ConcurrentQueue<Task>();

            forEachAsyncHandlingOption = new ForEachAsyncHandlingOption {MaxDegreeOfParallelism = 2};
            mockMessageHandler.Setup(p => p.HandleAsync(It.IsAny<TestMessage>(), It.IsAny<ILambdaContext>()))
                .Returns(async ()=>
                {
                    var t = Task.Delay(1);
                    cq.Enqueue(t);
                    if (cq.Count > 2)
                    {
                        throw new Exception("not good");
                    }
                    await t;
                    cq.TryDequeue(out t);
                });

            var sut = CreateSystemUnderTest();

            await sut.HandleAsync(sqsEvent, new TestLambdaContext());

            mockMessageHandler.VerifyAll();
            mockMessageHandler.Verify(
                handler => handler.HandleAsync(It.IsAny<TestMessage>(), It.IsAny<ILambdaContext>()),
                Times.Exactly(sqsEvent.Records.Count));
        }

        [Test]
        public void MaxDegreeOfParallelism_Should_ProperlyPropagated_And_Limited_To_Set_Max()
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

            var cq = new ConcurrentQueue<Task>();
            
            //We are checking if parallelism actually does what it's supposed to do. So we should have more then 2 concurrent processes running
            forEachAsyncHandlingOption = new ForEachAsyncHandlingOption { MaxDegreeOfParallelism = 4 };
            mockMessageHandler.Setup(p => p.HandleAsync(It.IsAny<TestMessage>(), It.IsAny<ILambdaContext>()))
                .Returns(async () =>
                {
                    var t = Task.Delay(1);
                    cq.Enqueue(t);
                    Console.WriteLine(cq.Count);
                    if (cq.Count > 2)
                    {
                        throw new Exception("Concurrent Tasks exceeded 2");
                    }
                    await t;
                    cq.TryDequeue(out t);
                });
            
            var sut = CreateSystemUnderTest();

            Assert.ThrowsAsync<Exception>(() => sut.HandleAsync(sqsEvent, new TestLambdaContext()));
        }
    }
}
