using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.TestUtilities;

using Kralizek.Lambda;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

namespace Tests.Lambda.Sqs;

[TestFixture]
public class SqsFunctionTests
{
    [SetUp]
    public void SetUp() => TestHandler.Reset();

    [Test]
    public async Task Function_decodes_messages_and_forwards_record_context()
    {
        var function = new TestFunction();
        var lambdaContext = TestLambdaContexts.Create();
        var @event = CreateEvent(
            ("first", "{\"value\":\"one\"}"),
            ("second", "{\"value\":\"two\"}"));
        @event.Records[1].ReceiptHandle = "receipt";
        @event.Records[1].EventSource = "aws:sqs";
        @event.Records[1].EventSourceArn = "arn:aws:sqs:eu-north-1:123456789012:orders";
        @event.Records[1].AwsRegion = "eu-north-1";
        @event.Records[1].Attributes = new Dictionary<string, string>
        {
            ["ApproximateReceiveCount"] = "2"
        };
        @event.Records[1].MessageAttributes = new Dictionary<string, SQSEvent.MessageAttribute>
        {
            ["tenant"] = new() { StringValue = "example" }
        };

        var response = await function.FunctionHandlerAsync(@event, lambdaContext);

        Assert.Multiple(() =>
        {
            Assert.That(response.BatchItemFailures, Is.Empty);
            Assert.That(TestHandler.Messages.Select(message => message.Value), Is.EquivalentTo(new[] { "one", "two" }));
            Assert.That(TestHandler.LastContext?.MessageId, Is.EqualTo("second"));
            Assert.That(TestHandler.LastContext?.ReceiptHandle, Is.EqualTo("receipt"));
            Assert.That(TestHandler.LastContext?.EventSource, Is.EqualTo("aws:sqs"));
            Assert.That(TestHandler.LastContext?.EventSourceArn, Is.EqualTo("arn:aws:sqs:eu-north-1:123456789012:orders"));
            Assert.That(TestHandler.LastContext?.AwsRegion, Is.EqualTo("eu-north-1"));
            Assert.That(TestHandler.LastContext?.Attributes["ApproximateReceiveCount"], Is.EqualTo("2"));
            Assert.That(TestHandler.LastContext?.MessageAttributes["tenant"].StringValue, Is.EqualTo("example"));
            Assert.That(TestHandler.LastContext?.GetSqsMessage(), Is.SameAs(@event.Records[1]));
            Assert.That(TestHandler.LastContext?.AwsRequestId, Is.EqualTo(lambdaContext.AwsRequestId));
            Assert.That(TestHandler.LastContext?.GetLambdaContext(), Is.SameAs(lambdaContext));
        });
    }

    [Test]
    public async Task Function_reports_only_failed_records()
    {
        var function = new TestFunction();
        var @event = CreateEvent(
            ("ok", "{\"value\":\"ok\"}"),
            ("failed", "{\"value\":\"fail\"}"));

        var response = await function.FunctionHandlerAsync(@event, TestLambdaContexts.Create());

        Assert.That(
            response.BatchItemFailures.Select(failure => failure.ItemIdentifier),
            Is.EqualTo(new[] { "failed" }));
    }

    [Test]
    public void Failure_result_preserves_reason_and_union_case_value()
    {
        var result = SqsRecordResult.Failed("not ready");

        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.TypeOf<SqsRecordResult.FailureCase>());
            Assert.That(((SqsRecordResult.FailureCase)result.Value!).Reason, Is.EqualTo("not ready"));
        });
    }

    [Test]
    public void Success_result_contains_success_union_case_value()
    {
        Assert.That(SqsRecordResult.Success.Value, Is.TypeOf<SqsRecordResult.SuccessCase>());
    }

    [Test]
    public async Task Function_treats_null_records_as_empty_batch()
    {
        var function = new TestFunction();
        var @event = new SQSEvent { Records = null! };

        var response = await function.FunctionHandlerAsync(@event, TestLambdaContexts.Create());

        Assert.Multiple(() =>
        {
            Assert.That(response.BatchItemFailures, Is.Empty);
            Assert.That(TestHandler.Messages, Is.Empty);
        });
    }

    [Test]
    public async Task Consumer_can_replace_default_decoder()
    {
        var function = new PlainTextFunction();
        var @event = CreateEvent(("text", "plain text"));

        var response = await function.FunctionHandlerAsync(@event, TestLambdaContexts.Create());

        Assert.Multiple(() =>
        {
            Assert.That(response.BatchItemFailures, Is.Empty);
            Assert.That(TestHandler.Messages.Single().Value, Is.EqualTo("plain text"));
        });
    }

    [Test]
    public void Invocation_cancellation_aborts_the_batch()
    {
        var function = new TestFunction();
        var context = new TestLambdaContext { RemainingTime = TimeSpan.Zero };
        var @event = CreateEvent(("first", "{\"value\":\"one\"}"));

        Assert.That(
            async () => await function.FunctionHandlerAsync(@event, context),
            Throws.TypeOf<OperationCanceledException>());
    }

    [Test]
    public async Task Parallel_function_processes_all_records()
    {
        var function = new TestParallelFunction();
        var @event = CreateEvent(
            ("first", "{\"value\":\"one\"}"),
            ("second", "{\"value\":\"two\"}"),
            ("third", "{\"value\":\"three\"}"));

        var response = await function.FunctionHandlerAsync(@event, TestLambdaContexts.Create());

        Assert.Multiple(() =>
        {
            Assert.That(response.BatchItemFailures, Is.Empty);
            Assert.That(TestHandler.Messages.Select(message => message.Value), Is.EquivalentTo(new[] { "one", "two", "three" }));
        });
    }

    private static SQSEvent CreateEvent(params (string Id, string Body)[] records) =>
        new()
        {
            Records = records
                .Select(record => new SQSEvent.SQSMessage
                {
                    MessageId = record.Id,
                    Body = record.Body
                })
                .ToList()
        };

    private sealed class TestFunction : SqsFunction<TestMessage, TestHandler>;

    private sealed class TestParallelFunction : ParallelSqsFunction<TestMessage, TestHandler>
    {
        protected override int MaxDegreeOfParallelism => 2;
    }

    private sealed class PlainTextFunction : SqsFunction<TestMessage, TestHandler>
    {
        protected override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IStringPayloadDecoder<TestMessage>, PlainTextTestDecoder>();
        }
    }

    private sealed class PlainTextTestDecoder : IStringPayloadDecoder<TestMessage>
    {
        public ValueTask<TestMessage> DecodeAsync(string payload, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(new TestMessage(payload));
        }
    }

    private sealed class TestHandler : ISqsMessageHandler<TestMessage>
    {
        private static readonly ConcurrentQueue<TestMessage> ReceivedMessages = new();

        public static IReadOnlyCollection<TestMessage> Messages => ReceivedMessages.ToArray();

        public static SqsMessageContext? LastContext { get; private set; }

        public static void Reset()
        {
            ReceivedMessages.Clear();
            LastContext = null;
        }

        public ValueTask<SqsRecordResult> HandleAsync(
            TestMessage message,
            SqsMessageContext context,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastContext = context;

            if (message.Value == "fail")
            {
                return ValueTask.FromResult(SqsRecordResult.Failed("Expected test failure."));
            }

            ReceivedMessages.Enqueue(message);
            return ValueTask.FromResult(SqsRecordResult.Success);
        }
    }

    private sealed record TestMessage(string Value);
}