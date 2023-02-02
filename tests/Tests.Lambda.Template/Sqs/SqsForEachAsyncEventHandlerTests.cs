using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.TestUtilities;
using Kralizek.Lambda;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace Tests.Lambda.Sqs;

[TestFixture]
public class ParallelSqsEventHandlerTests
{
    private Mock<IMessageSerializer> _mockMessageSerializer;
    private Mock<IMessageHandler<TestMessage>> _mockMessageHandler;
    private Mock<IServiceScopeFactory> _mockServiceScopeFactory;
    private Mock<IServiceProvider> _mockServiceProvider;
    private Mock<ILoggerFactory> _mockLoggerFactory;
    private Mock<IServiceScope> _mockServiceScope;
    private ParallelSqsExecutionOptions _parallelExecutionOptions;

    [SetUp]
    public void Initialize()
    {
        _mockMessageSerializer = new Mock<IMessageSerializer>();

        _mockMessageSerializer
            .Setup(p => p.Deserialize<TestMessage>(It.IsAny<string>()))
            .Returns(() => new TestMessage());
            
        _mockMessageHandler = new Mock<IMessageHandler<TestMessage>>();
        _mockMessageHandler.Setup(p => p.HandleAsync(It.IsAny<TestMessage>(), It.IsAny<ILambdaContext>())).Returns(Task.CompletedTask);

        _mockServiceScope = new Mock<IServiceScope>();

        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();

        _mockServiceScopeFactory.Setup(p => p.CreateScope()).Returns(_mockServiceScope.Object);

        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockServiceProvider.Setup(p => p.GetService(typeof(IMessageHandler<TestMessage>)))
            .Returns(_mockMessageHandler.Object);
        _mockServiceProvider.Setup(p => p.GetService(typeof(IServiceScopeFactory)))
            .Returns(_mockServiceScopeFactory.Object);

        _mockServiceProvider
            .Setup(p => p.GetService(typeof(IMessageSerializer)))
            .Returns(_mockMessageSerializer.Object);

        _mockServiceScope.Setup(p => p.ServiceProvider).Returns(_mockServiceProvider.Object);

        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLoggerFactory.Setup(p => p.CreateLogger(It.IsAny<string>()))
            .Returns(Mock.Of<ILogger>());

        _parallelExecutionOptions = new ParallelSqsExecutionOptions { MaxDegreeOfParallelism = 4 };
    }

    private ParallelSqsEventHandler<TestMessage> CreateSystemUnderTest() =>
        CreateSystemUnderTest<ParallelSqsEventHandler<TestMessage>>();

    private THandler CreateSystemUnderTest<THandler>() where THandler : class
    {
        var handler = new ParallelSqsEventHandler<TestMessage>(_mockServiceProvider.Object, _mockLoggerFactory.Object, Options.Create(_parallelExecutionOptions)) as THandler;
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

        _mockServiceProvider.Verify(p => p.GetService(typeof(IMessageHandler<TestMessage>)), Times.Exactly(sqsEvent.Records.Count));
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

        _mockServiceScopeFactory.Verify(p => p.CreateScope(), Times.Exactly(sqsEvent.Records.Count));
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

        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockServiceProvider.Setup(p => p.GetService(typeof(IServiceScopeFactory))).Returns(_mockServiceScopeFactory.Object);

        _mockServiceScope.Setup(p => p.ServiceProvider).Returns(_mockServiceProvider.Object);

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

        _parallelExecutionOptions = new ParallelSqsExecutionOptions {MaxDegreeOfParallelism = 2};
        _mockMessageHandler.Setup(p => p.HandleAsync(It.IsAny<TestMessage>(), It.IsAny<ILambdaContext>()))
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

        _mockMessageHandler.VerifyAll();
        _mockMessageHandler.Verify(
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
        _parallelExecutionOptions = new ParallelSqsExecutionOptions { MaxDegreeOfParallelism = 4 };
        _mockMessageHandler.Setup(p => p.HandleAsync(It.IsAny<TestMessage>(), It.IsAny<ILambdaContext>()))
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

    [Test]
    public void HandleAsync_lets_NotificationHandler_exceptions_fly_when_not_using_sqs_batch_response()
    {
        _mockMessageHandler = new Mock<IMessageHandler<TestMessage>>();
        _mockMessageHandler.Setup(p => p.HandleAsync(It.IsAny<TestMessage>(), It.IsAny<ILambdaContext>()))
            .Returns(Task.FromException(new InvalidDataException()));

        _mockServiceProvider.Setup(p => p.GetService(typeof(IMessageHandler<TestMessage>)))
           .Returns(_mockMessageHandler.Object);

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
        _mockMessageHandler = new Mock<IMessageHandler<TestMessage>>();
        _mockMessageHandler.Setup(p => p.HandleAsync(It.IsAny<TestMessage>(), It.IsAny<ILambdaContext>()))
            .Returns(testErrors ? Task.FromException(new InvalidDataException()) : Task.CompletedTask);

        _mockServiceProvider.Setup(p => p.GetService(typeof(IMessageHandler<TestMessage>)))
           .Returns(_mockMessageHandler.Object);

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

        _mockServiceScopeFactory.Verify(p => p.CreateScope(), Times.Exactly(sqsEvent.Records.Count));
        Assert.That(batchResponse?.BatchItemFailures, Is.Not.Null);

        var expectedBatchFailures = testErrors ? new string[] { "msg1", "msg2" } : Array.Empty<string>();
        Assert.That(batchResponse.BatchItemFailures.Select(x => x.ItemIdentifier), Is.EquivalentTo(expectedBatchFailures));
    }
}