using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.SNSEvents;

using Kralizek.Lambda;

using NUnit.Framework;

namespace Tests.Lambda.Sns;

[TestFixture]
public class RawSnsFunctionTests
{
    [SetUp]
    public void SetUp() => TestHandler.Reset();

    [Test]
    public async Task Function_forwards_raw_records_and_context()
    {
        var function = new TestFunction();
        var lambdaContext = TestLambdaContexts.Create();
        var @event = CreateEvent("first", "second");

        await function.FunctionHandlerAsync(@event, lambdaContext);

        Assert.Multiple(() =>
        {
            Assert.That(TestHandler.Records.Select(record => record.Sns.MessageId), Is.EqualTo(new[] { "first", "second" }));
            Assert.That(TestHandler.LastRecord, Is.SameAs(@event.Records[1]));
            Assert.That(TestHandler.LastContext?.GetSnsRecord(), Is.SameAs(@event.Records[1]));
            Assert.That(TestHandler.LastContext?.MessageId, Is.EqualTo("second"));
            Assert.That(TestHandler.LastContext?.GetLambdaContext(), Is.SameAs(lambdaContext));
        });
    }

    [Test]
    public void Record_failure_fails_the_whole_invocation()
    {
        var function = new TestFunction();
        var @event = CreateEvent("ok", "failed");

        Assert.That(
            async () => await function.FunctionHandlerAsync(@event, TestLambdaContexts.Create()),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public async Task Parallel_function_processes_all_records()
    {
        var function = new TestParallelFunction();
        var @event = CreateEvent("first", "second", "third");

        await function.FunctionHandlerAsync(@event, TestLambdaContexts.Create());

        Assert.That(
            TestHandler.Records.Select(record => record.Sns.MessageId),
            Is.EquivalentTo(new[] { "first", "second", "third" }));
    }

    private static SNSEvent CreateEvent(params string[] messageIds) =>
        new()
        {
            Records = messageIds
                .Select(messageId => new SNSEvent.SNSRecord
                {
                    Sns = new SNSEvent.SNSMessage
                    {
                        MessageId = messageId,
                        Message = messageId,
                        Timestamp = DateTime.UtcNow
                    }
                })
                .ToList()
        };

    private sealed class TestFunction : SnsFunction<TestHandler>;

    private sealed class TestParallelFunction : ParallelSnsFunction<TestHandler>
    {
        protected override int MaxDegreeOfParallelism => 2;
    }

    private sealed class TestHandler : ISnsRecordHandler
    {
        private static readonly ConcurrentQueue<SNSEvent.SNSRecord> ReceivedRecords = new();

        public static IReadOnlyCollection<SNSEvent.SNSRecord> Records => ReceivedRecords.ToArray();

        public static SNSEvent.SNSRecord? LastRecord { get; private set; }

        public static SnsNotificationContext? LastContext { get; private set; }

        public static void Reset()
        {
            ReceivedRecords.Clear();
            LastRecord = null;
            LastContext = null;
        }

        public ValueTask HandleAsync(
            SNSEvent.SNSRecord record,
            SnsNotificationContext context,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (record.Sns.MessageId == "failed")
            {
                throw new InvalidOperationException("Expected test failure.");
            }

            LastRecord = record;
            LastContext = context;
            ReceivedRecords.Enqueue(record);
            return ValueTask.CompletedTask;
        }
    }
}