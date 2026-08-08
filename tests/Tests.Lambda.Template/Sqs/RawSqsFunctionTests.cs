using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.SQSEvents;

using Kralizek.Lambda;

using NUnit.Framework;

namespace Tests.Lambda.Sqs;

[TestFixture]
public class RawSqsFunctionTests
{
    [SetUp]
    public void SetUp() => TestRecordHandler.Reset();

    [Test]
    public async Task Function_forwards_raw_record_and_context()
    {
        var function = new TestFunction();
        var lambdaContext = TestLambdaContexts.Create();
        var record = new SQSEvent.SQSMessage
        {
            MessageId = "first",
            Body = "raw body",
            ReceiptHandle = "receipt",
            EventSourceArn = "arn:aws:sqs:eu-north-1:123456789012:orders"
        };
        var @event = new SQSEvent { Records = new List<SQSEvent.SQSMessage> { record } };

        var response = await function.FunctionHandlerAsync(@event, lambdaContext);

        Assert.Multiple(() =>
        {
            Assert.That(response.BatchItemFailures, Is.Empty);
            Assert.That(TestRecordHandler.Records.Single(), Is.SameAs(record));
            Assert.That(TestRecordHandler.LastContext?.MessageId, Is.EqualTo("first"));
            Assert.That(TestRecordHandler.LastContext?.ReceiptHandle, Is.EqualTo("receipt"));
            Assert.That(TestRecordHandler.LastContext?.EventSourceArn, Is.EqualTo(record.EventSourceArn));
            Assert.That(TestRecordHandler.LastContext?.GetSqsMessage(), Is.SameAs(record));
            Assert.That(TestRecordHandler.LastContext?.GetLambdaContext(), Is.SameAs(lambdaContext));
        });
    }

    [Test]
    public async Task Function_reports_failed_raw_record()
    {
        var function = new TestFunction();
        var @event = CreateEvent(
            ("ok", "ok"),
            ("failed", "fail"));

        var response = await function.FunctionHandlerAsync(@event, TestLambdaContexts.Create());

        Assert.That(
            response.BatchItemFailures.Select(failure => failure.ItemIdentifier),
            Is.EqualTo(new[] { "failed" }));
    }

    [Test]
    public async Task Parallel_function_processes_raw_records()
    {
        var function = new TestParallelFunction();
        var @event = CreateEvent(
            ("first", "one"),
            ("second", "two"),
            ("third", "three"));

        var response = await function.FunctionHandlerAsync(@event, TestLambdaContexts.Create());

        Assert.Multiple(() =>
        {
            Assert.That(response.BatchItemFailures, Is.Empty);
            Assert.That(
                TestRecordHandler.Records.Select(record => record.MessageId),
                Is.EquivalentTo(new[] { "first", "second", "third" }));
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

    private sealed class TestFunction : SqsFunction<TestRecordHandler>;

    private sealed class TestParallelFunction : ParallelSqsFunction<TestRecordHandler>
    {
        protected override int MaxDegreeOfParallelism => 2;
    }

    private sealed class TestRecordHandler : ISqsRecordHandler
    {
        private static readonly ConcurrentQueue<SQSEvent.SQSMessage> ReceivedRecords = new();

        public static IReadOnlyCollection<SQSEvent.SQSMessage> Records => ReceivedRecords.ToArray();

        public static SqsMessageContext? LastContext { get; private set; }

        public static void Reset()
        {
            ReceivedRecords.Clear();
            LastContext = null;
        }

        public ValueTask HandleAsync(
            SQSEvent.SQSMessage record,
            SqsMessageContext context,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastContext = context;

            if (record.Body == "fail")
            {
                throw new InvalidOperationException("Expected test failure.");
            }

            ReceivedRecords.Enqueue(record);
            return ValueTask.CompletedTask;
        }
    }
}