using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.TestUtilities;
using Kralizek.Lambda;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Tests.Lambda.Sns
{
    public class SnsForEachAsyncEventHandlerTests
    {
        private Mock<INotificationHandler<TestNotification>> mockNotificationHandler;
        private Mock<IServiceScopeFactory> mockServiceScopeFactory;
        private Mock<IServiceProvider> mockServiceProvider;
        private Mock<ILoggerFactory> mockLoggerFactory;
        private Mock<IServiceScope> mockServiceScope;
        private ForEachAsyncHandlingOption forEachAsyncHandlingOption;

        [SetUp]
        public void Initialize()
        {
            mockNotificationHandler = new Mock<INotificationHandler<TestNotification>>();
            mockNotificationHandler.Setup(p => p.HandleAsync(It.IsAny<TestNotification>(), It.IsAny<ILambdaContext>()))
                .Returns(Task.CompletedTask);

            mockServiceScope = new Mock<IServiceScope>();

            mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
            mockServiceScopeFactory.Setup(p => p.CreateScope()).Returns(mockServiceScope.Object);

            mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider.Setup(p => p.GetService(typeof(INotificationHandler<TestNotification>)))
                .Returns(mockNotificationHandler.Object);
            mockServiceProvider.Setup(p => p.GetService(typeof(IServiceScopeFactory)))
                .Returns(mockServiceScopeFactory.Object);

            mockServiceScope.Setup(p => p.ServiceProvider).Returns(mockServiceProvider.Object);


            mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(p => p.CreateLogger(It.IsAny<string>()))
                .Returns(Mock.Of<ILogger>());

            forEachAsyncHandlingOption = new ForEachAsyncHandlingOption { MaxDegreeOfParallelism = 4 };

        }

        private SnsForEachAsyncEventHandler<TestNotification> CreateSystemUnderTest()
        {
            return new SnsForEachAsyncEventHandler<TestNotification>(mockServiceProvider.Object, mockLoggerFactory.Object, forEachAsyncHandlingOption);
        }

        [Test]
        public async Task HandleAsync_resolves_MessageHandler_for_each_record()
        {
            var snsEvent = new SNSEvent
            {
                Records = new List<SNSEvent.SNSRecord>
                {
                    new SNSEvent.SNSRecord
                    {
                        Sns = new SNSEvent.SNSMessage
                        {
                            Message = "{}"
                        }
                    }, 
                    new SNSEvent.SNSRecord
                    {
                        Sns = new SNSEvent.SNSMessage
                        {
                            Message = "{}"
                        }
                    },
                    new SNSEvent.SNSRecord
                    {
                        Sns = new SNSEvent.SNSMessage
                        {
                            Message = "{}"
                        }
                    },
                    new SNSEvent.SNSRecord
                    {
                        Sns = new SNSEvent.SNSMessage
                        {
                            Message = "{}"
                        }
                    }
                }
            };

            var lambdaContext = new TestLambdaContext();

            var sut = CreateSystemUnderTest();

            await sut.HandleAsync(snsEvent, lambdaContext);

            mockServiceProvider.Verify(p => p.GetService(typeof(INotificationHandler<TestNotification>)), Times.Exactly(snsEvent.Records.Count));
        }

        [Test]
        public async Task HandleAsync_resolves_NotificationHandler_for_each_record()
        {
            var snsEvent = new SNSEvent
            {
                Records = new List<SNSEvent.SNSRecord>
                {
                    new SNSEvent.SNSRecord
                    {
                        Sns = new SNSEvent.SNSMessage
                        {
                            Message = "{}"
                        }
                    },
                    new SNSEvent.SNSRecord
                    {
                        Sns = new SNSEvent.SNSMessage
                        {
                            Message = "{}"
                        }
                    }
                }
            };

            var lambdaContext = new TestLambdaContext();

            var sut = CreateSystemUnderTest();

            await sut.HandleAsync(snsEvent, lambdaContext);

            mockServiceProvider.Verify(p => p.GetService(typeof(INotificationHandler<TestNotification>)), Times.Exactly(snsEvent.Records.Count));
        }

        [Test]
        public void HandleAsync_throws_InvalidOperation_if_NotificationHandler_is_not_registered()
        {
            var snsEvent = new SNSEvent
            {
                Records = new List<SNSEvent.SNSRecord>
                {
                    new SNSEvent.SNSRecord
                    {
                        Sns = new SNSEvent.SNSMessage
                        {
                            Message = "{}"
                        }
                    },
                    new SNSEvent.SNSRecord
                    {
                        Sns = new SNSEvent.SNSMessage
                        {
                            Message = "{}"
                        }
                    }
                }
            };

            var lambdaContext = new TestLambdaContext();

            mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider.Setup(p => p.GetService(typeof(IServiceScopeFactory))).Returns(mockServiceScopeFactory.Object);
            mockServiceScope.Setup(p => p.ServiceProvider).Returns(mockServiceProvider.Object);

            var sut = CreateSystemUnderTest();

            Assert.ThrowsAsync<InvalidOperationException>(() => sut.HandleAsync(snsEvent, lambdaContext));
        }

        [Test]
        public async Task MaxDegreeOfParallelism_Should_ProperlyPropagated()
        {
            var snsEvent = new SNSEvent
            {
                Records = new List<SNSEvent.SNSRecord>
                {
                    new SNSEvent.SNSRecord
                    {
                        Sns = new SNSEvent.SNSMessage
                        {
                            Message = "{}"
                        }
                    },
                    new SNSEvent.SNSRecord
                    {
                        Sns = new SNSEvent.SNSMessage
                        {
                            Message = "{}"
                        }
                    },
                    new SNSEvent.SNSRecord
                    {
                        Sns = new SNSEvent.SNSMessage
                        {
                            Message = "{}"
                        }
                    },
                    new SNSEvent.SNSRecord
                    {
                        Sns = new SNSEvent.SNSMessage
                        {
                            Message = "{}"
                        }
                    }
                }
            };

            var cq = new ConcurrentQueue<Task>();

            forEachAsyncHandlingOption = new ForEachAsyncHandlingOption { MaxDegreeOfParallelism = 2 };
            mockNotificationHandler.Setup(p => p.HandleAsync(It.IsAny<TestNotification>(), It.IsAny<ILambdaContext>()))
                .Returns(async () =>
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

            await sut.HandleAsync(snsEvent, new TestLambdaContext());

            mockNotificationHandler.VerifyAll();
            mockNotificationHandler.Verify(
                handler => handler.HandleAsync(It.IsAny<TestNotification>(), It.IsAny<ILambdaContext>()),
                Times.Exactly(snsEvent.Records.Count));
        }

        [Test]
        public void MaxDegreeOfParallelism_Should_ProperlyPropagated_And_Limited_To_Set_Max()
        {
            var snsEvent = new SNSEvent
            {
                Records = new List<SNSEvent.SNSRecord>
                {
                    new SNSEvent.SNSRecord
                    {
                        Sns = new SNSEvent.SNSMessage
                        {
                            Message = "{}"
                        }
                    },
                    new SNSEvent.SNSRecord
                    {
                        Sns = new SNSEvent.SNSMessage
                        {
                            Message = "{}"
                        }
                    },
                    new SNSEvent.SNSRecord
                    {
                        Sns = new SNSEvent.SNSMessage
                        {
                            Message = "{}"
                        }
                    },
                    new SNSEvent.SNSRecord
                    {
                        Sns = new SNSEvent.SNSMessage
                        {
                            Message = "{}"
                        }
                    }
                }
            };

            var cq = new ConcurrentQueue<Task>();

            //We are checking if parallelism actually does what it's supposed to do. So we should have more then 2 concurrent processes running
            forEachAsyncHandlingOption = new ForEachAsyncHandlingOption { MaxDegreeOfParallelism = 4 };
            mockNotificationHandler.Setup(p => p.HandleAsync(It.IsAny<TestNotification>(), It.IsAny<ILambdaContext>()))
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

            Assert.ThrowsAsync<Exception>(() => sut.HandleAsync(snsEvent, new TestLambdaContext()));
        }
    }
}
