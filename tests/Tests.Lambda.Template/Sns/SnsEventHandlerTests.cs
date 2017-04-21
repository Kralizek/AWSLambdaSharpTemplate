using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Amazon.Lambda.TestUtilities;
using Kralizek.Lambda;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.Lambda.Sns
{
    public class SnsEventHandlerTests
    {
        private Mock<INotificationHandler<TestNotification>> mockNotificationHandler;
        private Mock<IServiceScopeFactory> mockServiceScopeFactory;
        private Mock<IServiceProvider> mockServiceProvider;
        private Mock<ILoggerFactory> mockLoggerFactory;

        public SnsEventHandlerTests()
        {
            mockNotificationHandler = new Mock<INotificationHandler<TestNotification>>();
            mockNotificationHandler.Setup(p => p.HandleAsync(It.IsAny<TestNotification>(), It.IsAny<ILambdaContext>()))
                                    .Returns(Task.CompletedTask);
            
            mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
            mockServiceScopeFactory.Setup(p => p.CreateScope()).Returns(Mock.Of<IServiceScope>());

            mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider.Setup(p => p.GetService(typeof(INotificationHandler<TestNotification>)))
                                    .Returns(mockNotificationHandler.Object);
            mockServiceProvider.Setup(p => p.GetService(typeof(IServiceScopeFactory)))
                                    .Returns(mockServiceScopeFactory.Object);

            mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(p => p.CreateLogger(It.IsAny<string>()))
                                .Returns(Mock.Of<ILogger>());
        }

        private SnsEventHandler<TestNotification> CreateSystemUnderTest()
        {
            return new SnsEventHandler<TestNotification>(mockServiceProvider.Object, mockLoggerFactory.Object);
        }

        [Fact]
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

        [Fact]
        public async Task HandleAsync_creates_a_scope_for_each_record()
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

            mockServiceScopeFactory.Verify(p => p.CreateScope(), Times.Exactly(snsEvent.Records.Count));
        }

        [Fact]
        public async Task HandleAsync_executes_NotificationHandler_for_each_record()
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

            mockNotificationHandler.Verify(p => p.HandleAsync(It.IsAny<TestNotification>(), lambdaContext), Times.Exactly(snsEvent.Records.Count));
        }

        [Fact]
        public async Task HandleAsync_throws_InvalidOperation_if_NotificationHandler_is_not_registered()
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

            var sut = CreateSystemUnderTest();

            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.HandleAsync(snsEvent, lambdaContext));
        }
    }
}
