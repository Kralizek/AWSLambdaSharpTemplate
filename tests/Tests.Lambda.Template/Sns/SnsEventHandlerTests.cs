﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Amazon.Lambda.TestUtilities;
using Kralizek.Lambda;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;


namespace Tests.Lambda.Sns
{
    [TestFixture]
    public class SnsEventHandlerTests
    {
        private Mock<INotificationHandler<TestNotification>> mockNotificationHandler;
        private Mock<IServiceScopeFactory> mockServiceScopeFactory;
        private Mock<IServiceProvider> mockServiceProvider;
        private Mock<ILoggerFactory> mockLoggerFactory;
        private Mock<IServiceScope> mockServiceScope;


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
        }

        private SnsEventHandler<TestNotification> CreateSystemUnderTest()
        {
            return new SnsEventHandler<TestNotification>(mockServiceProvider.Object, mockLoggerFactory.Object);
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

        [Test]
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
    }
}
