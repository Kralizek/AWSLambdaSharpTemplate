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
using NUnit.Framework;


namespace Tests.Lambda.Sns;

[TestFixture]
public class SnsEventHandlerTests
{
    private Mock<INotificationSerializer> _mockNotificationSerializer;
    private Mock<INotificationHandler<TestNotification>> _mockNotificationHandler;
    private Mock<IServiceScopeFactory> _mockServiceScopeFactory;
    private Mock<IServiceProvider> _mockServiceProvider;
    private Mock<ILoggerFactory> _mockLoggerFactory;
    private Mock<IServiceScope> _mockServiceScope;


    [SetUp]
    public void Initialize()
    {
        _mockNotificationSerializer = new Mock<INotificationSerializer>();
        _mockNotificationSerializer.Setup(p => p.Deserialize<TestNotification>(It.IsAny<string>())).Returns(() => new TestNotification());
            
        _mockNotificationHandler = new Mock<INotificationHandler<TestNotification>>();
        _mockNotificationHandler.Setup(p => p.HandleAsync(It.IsAny<TestNotification>(), It.IsAny<ILambdaContext>()))
            .Returns(Task.CompletedTask);

        _mockServiceScope = new Mock<IServiceScope>();

        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        _mockServiceScopeFactory.Setup(p => p.CreateScope()).Returns(_mockServiceScope.Object);

        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockServiceProvider.Setup(p => p.GetService(typeof(INotificationHandler<TestNotification>)))
            .Returns(_mockNotificationHandler.Object);
        _mockServiceProvider.Setup(p => p.GetService(typeof(IServiceScopeFactory)))
            .Returns(_mockServiceScopeFactory.Object);
            
        _mockServiceProvider
            .Setup(p => p.GetService(typeof(INotificationSerializer)))
            .Returns(_mockNotificationSerializer.Object);

        _mockServiceScope.Setup(p => p.ServiceProvider).Returns(_mockServiceProvider.Object);


        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLoggerFactory.Setup(p => p.CreateLogger(It.IsAny<string>()))
            .Returns(Mock.Of<ILogger>());
    }

    private SnsEventHandler<TestNotification> CreateSystemUnderTest()
    {
        return new SnsEventHandler<TestNotification>(_mockServiceProvider.Object, _mockLoggerFactory.Object);
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

        _mockServiceProvider.Verify(p => p.GetService(typeof(INotificationHandler<TestNotification>)), Times.Exactly(snsEvent.Records.Count));
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

        _mockServiceScopeFactory.Verify(p => p.CreateScope(), Times.Exactly(snsEvent.Records.Count));
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

        _mockNotificationHandler.Verify(p => p.HandleAsync(It.IsAny<TestNotification>(), lambdaContext), Times.Exactly(snsEvent.Records.Count));
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

        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockServiceProvider.Setup(p => p.GetService(typeof(IServiceScopeFactory))).Returns(_mockServiceScopeFactory.Object);
        _mockServiceScope.Setup(p => p.ServiceProvider).Returns(_mockServiceProvider.Object);

        var sut = CreateSystemUnderTest();

        Assert.ThrowsAsync<InvalidOperationException>(() => sut.HandleAsync(snsEvent, lambdaContext));
    }
}