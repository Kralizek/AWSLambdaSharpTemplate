using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.TestUtilities;
using Kralizek.Lambda;
using Kralizek.Lambda.Accessors;
using Kralizek.Lambda.Accessors.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Tests.Lambda.Sqs;

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

    private SqsEventHandler<TestMessage> CreateSystemUnderTest() =>
        CreateSystemUnderTest<SqsEventHandler<TestMessage>>();

    private THandler CreateSystemUnderTest<THandler>() where THandler : class
    {
        var handler = new SqsEventHandler<TestMessage>(mockServiceProvider.Object, mockLoggerFactory.Object) as THandler;
        if (handler is null)
        {
            throw new InvalidOperationException($"system under test {nameof(THandler)} type {typeof(THandler)} not valid");
        }

        return handler;
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
    public async Task HandleAsync_provides_scoped_sqs_record_accessor_for_each_record()
    {
        ISqsRecordAccessor currentAccessor = null;
        var records = new HashSet<SqsRecord>();

        mockMessageHandler.Setup(p => p.HandleAsync(It.IsAny<TestMessage>(), It.IsAny<ILambdaContext>())).Returns(Task.CompletedTask).Callback<TestMessage, ILambdaContext>((x, y) =>
        {
            records.Add(currentAccessor.SqsRecord);
        });

        mockServiceScopeFactory.Setup(p => p.CreateScope()).Returns(mockServiceScope.Object).Callback(() =>
        {
            var internalAccessor = currentAccessor = new SqsRecordAccessor();
            mockServiceProvider.Setup(p => p.GetService(typeof(SqsRecordAccessor)))
                .Returns(internalAccessor);
            mockServiceProvider.Setup(p => p.GetService(typeof(ISqsRecordAccessor)))
                .Returns(internalAccessor);
        });

        mockServiceScope.Setup(s => s.Dispose()).Callback(() => currentAccessor = null);

        var sqsEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new SQSEvent.SQSMessage
                {
                    MessageId = "msg1",
                    ReceiptHandle = "AQEBwJnKyrHigUMZj6rYigCgxlaS3SLy0a...",
                    Body = "{}",
                    Md5OfBody = "99914b932bd37a50b983c5e7c90ae93b",
                    Attributes = new Dictionary<string, string>() {["ApproximateReceiveCount"] = "1", ["SentTimestamp"] = "1545082649183"}
                },
                new SQSEvent.SQSMessage
                {
                    MessageId = "msg2",
                    ReceiptHandle = "AQEBzWwaftRI0KuVm4tP+/7q1rGgNqicHq...",
                    Body = "{}",
                    Md5OfBody = "99914b932bd37a50b983c5e7c90ae93b",
                    Attributes = new Dictionary<string, string>() {["ApproximateReceiveCount"] = "1", ["SentTimestamp"] = "1545082650636"}
                },
            }
        };

        var lambdaContext = new TestLambdaContext();

        var sut = CreateSystemUnderTest();

        await sut.HandleAsync(sqsEvent, lambdaContext);
        mockMessageHandler.Verify(p => p.HandleAsync(It.IsAny<TestMessage>(), It.IsAny<ILambdaContext>()), Times.Exactly(sqsEvent.Records.Count));
        mockServiceScope.Verify(p => p.Dispose(), Times.Exactly(sqsEvent.Records.Count));

        Assert.That(records, Has.Count.EqualTo(sqsEvent.Records.Count));
        Assert.That(records.Select(x => x.MessageId), Is.EquivalentTo(new string[] { "msg1", "msg2" }));

        SqsRecord record = records.Where(x => x.MessageId == "msg1").Single();
        SQSEvent.SQSMessage rawrecord = sqsEvent.Records[0];
        SqsRecord expectedRecord = new SqsRecord(rawrecord.MessageId, rawrecord.ReceiptHandle, rawrecord.Md5OfBody, null, null, null);
        expectedRecord = expectedRecord // msg1 SentTimestamp is 1545082649183 epoch ms
            with { ApproximateReceiveCount = 1, SentTimestamp = DateTimeOffset.ParseExact("2018-12-17T21:37:29.1830000+00:00", "O", CultureInfo.InvariantCulture) };

        Assert.That(record, Is.EqualTo(expectedRecord));
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

    [Test]
    public void HandleAsync_lets_NotificationHandler_exceptions_fly_when_not_using_sqs_batch_response()
    {
        mockMessageHandler = new Mock<IMessageHandler<TestMessage>>();
        mockMessageHandler.Setup(p => p.HandleAsync(It.IsAny<TestMessage>(), It.IsAny<ILambdaContext>()))
            .Returns(Task.FromException(new InvalidDataException()));

        mockServiceProvider.Setup(p => p.GetService(typeof(IMessageHandler<TestMessage>)))
           .Returns(mockMessageHandler.Object);

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

        Assert.ThrowsAsync<InvalidDataException>(() => sut.HandleAsync(sqsEvent, lambdaContext));
    }

    [Theory]
    public async Task HandleAsync_provides_sqs_batch_response(bool testErrors)
    {
        mockMessageHandler = new Mock<IMessageHandler<TestMessage>>();
        mockMessageHandler.Setup(p => p.HandleAsync(It.IsAny<TestMessage>(), It.IsAny<ILambdaContext>()))
            .Returns(testErrors ? Task.FromException(new InvalidDataException()) : Task.CompletedTask);

        mockServiceProvider.Setup(p => p.GetService(typeof(IMessageHandler<TestMessage>)))
           .Returns(mockMessageHandler.Object);

        var sqsEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new SQSEvent.SQSMessage
                {
                    MessageId = "msg1",
                    Body = "{}"
                },
                new SQSEvent.SQSMessage
                {
                    MessageId = "msg2",
                    Body = "{}"
                },
            }
        };

        var lambdaContext = new TestLambdaContext();

        var sut = CreateSystemUnderTest<IRequestResponseHandler<SQSEvent, SQSBatchResponse>>();

        SQSBatchResponse batchResponse = await sut.HandleAsync(sqsEvent, lambdaContext);

        mockServiceScopeFactory.Verify(p => p.CreateScope(), Times.Exactly(sqsEvent.Records.Count));
        Assert.That(batchResponse?.BatchItemFailures, Is.Not.Null);

        var expectedBatchFailures = testErrors ? new string[] { "msg1", "msg2" } : Array.Empty<string>();
        Assert.That(batchResponse.BatchItemFailures.Select(x => x.ItemIdentifier), Is.EquivalentTo(expectedBatchFailures));
    }
}