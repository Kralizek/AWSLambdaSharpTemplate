using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.DynamoDBEvents;

using Kralizek.Lambda;

using NUnit.Framework;

namespace Tests.Lambda.DynamoDb;

[TestFixture]
public class DynamoDbStreamFunctionTests
{
    [SetUp]
    public void SetUp() => TestRecordHandler.Reset();

    [Test]
    public async Task Function_forwards_item_and_context()
    {
        var function = new TestFunction();
        var lambdaContext = TestLambdaContexts.Create();
        var record = CreateRecord("event-1", "101", "MODIFY");
        var @event = new DynamoDBEvent
        {
            Records = new List<DynamoDBEvent.DynamodbStreamRecord> { record }
        };

        var response = await function.FunctionHandlerAsync(@event, lambdaContext);

        var item = TestRecordHandler.Items.Single();

        Assert.Multiple(() =>
        {
            Assert.That(response.BatchItemFailures, Is.Empty);
            Assert.That(item.SequenceNumber, Is.EqualTo("101"));
            Assert.That(item.StreamViewType, Is.EqualTo(DynamoDbStreamViewType.NewAndOldImages));
            Assert.That(item.Keys["orderId"].S, Is.EqualTo("order-123"));
            Assert.That(item.NewImage["status"].S, Is.EqualTo("paid"));
            Assert.That(item.OldImage["status"].S, Is.EqualTo("pending"));
            Assert.That(TestRecordHandler.LastContext?.EventId, Is.EqualTo("event-1"));
            Assert.That(TestRecordHandler.LastContext?.EventName, Is.EqualTo("MODIFY"));
            Assert.That(TestRecordHandler.LastContext?.GetDynamoDbStreamRecord(), Is.SameAs(record));
            Assert.That(TestRecordHandler.LastContext?.GetLambdaContext(), Is.SameAs(lambdaContext));
        });
    }

    [TestCase("KEYS_ONLY", DynamoDbStreamViewType.KeysOnly)]
    [TestCase("NEW_IMAGE", DynamoDbStreamViewType.NewImage)]
    [TestCase("OLD_IMAGE", DynamoDbStreamViewType.OldImage)]
    [TestCase("NEW_AND_OLD_IMAGES", DynamoDbStreamViewType.NewAndOldImages)]
    [TestCase("FUTURE_VIEW", DynamoDbStreamViewType.Unknown)]
    [TestCase(null, DynamoDbStreamViewType.Unknown)]
    public async Task Function_maps_stream_view_type(
        string? streamViewType,
        DynamoDbStreamViewType expected)
    {
        var function = new TestFunction();
        var record = CreateRecord("event-1", "101", "MODIFY");
        record.Dynamodb!.StreamViewType = streamViewType;

        await function.FunctionHandlerAsync(
            new DynamoDBEvent
            {
                Records = new List<DynamoDBEvent.DynamodbStreamRecord> { record }
            },
            TestLambdaContexts.Create());

        Assert.That(TestRecordHandler.Items.Single().StreamViewType, Is.EqualTo(expected));
    }

    [Test]
    public async Task Function_reports_failed_record_by_sequence_number()
    {
        var function = new TestFunction();
        var @event = new DynamoDBEvent
        {
            Records = new List<DynamoDBEvent.DynamodbStreamRecord>
            {
                CreateRecord("event-1", "101", "MODIFY"),
                CreateRecord("event-2", "102", "FAIL")
            }
        };

        var response = await function.FunctionHandlerAsync(@event, TestLambdaContexts.Create());

        Assert.That(
            response.BatchItemFailures.Select(failure => failure.ItemIdentifier),
            Is.EqualTo(new[] { "102" }));
    }

    [Test]
    public async Task Parallel_function_processes_records()
    {
        var function = new TestParallelFunction();
        var @event = new DynamoDBEvent
        {
            Records = new List<DynamoDBEvent.DynamodbStreamRecord>
            {
                CreateRecord("event-1", "101", "INSERT"),
                CreateRecord("event-2", "102", "MODIFY"),
                CreateRecord("event-3", "103", "REMOVE")
            }
        };

        var response = await function.FunctionHandlerAsync(@event, TestLambdaContexts.Create());

        Assert.Multiple(() =>
        {
            Assert.That(response.BatchItemFailures, Is.Empty);
            Assert.That(
                TestRecordHandler.Items.Select(item => item.SequenceNumber),
                Is.EquivalentTo(new[] { "101", "102", "103" }));
        });
    }

    private static DynamoDBEvent.DynamodbStreamRecord CreateRecord(
        string eventId,
        string sequenceNumber,
        string eventName) =>
        new()
        {
            EventID = eventId,
            EventName = eventName,
            EventSource = "aws:dynamodb",
            EventSourceArn = "arn:aws:dynamodb:eu-north-1:123456789012:table/orders/stream/2026-08-08T00:00:00.000",
            AwsRegion = "eu-north-1",
            Dynamodb = new DynamoDBEvent.StreamRecord
            {
                SequenceNumber = sequenceNumber,
                StreamViewType = "NEW_AND_OLD_IMAGES",
                Keys = new Dictionary<string, DynamoDBEvent.AttributeValue>
                {
                    ["orderId"] = new() { S = "order-123" }
                },
                NewImage = new Dictionary<string, DynamoDBEvent.AttributeValue>
                {
                    ["status"] = new() { S = "paid" }
                },
                OldImage = new Dictionary<string, DynamoDBEvent.AttributeValue>
                {
                    ["status"] = new() { S = "pending" }
                }
            }
        };

    private sealed class TestFunction : DynamoDbStreamFunction<TestRecordHandler>;

    private sealed class TestParallelFunction : ParallelDynamoDbStreamFunction<TestRecordHandler>
    {
        protected override int MaxDegreeOfParallelism => 2;
    }

    private sealed class TestRecordHandler : IDynamoDbStreamRecordHandler
    {
        private static readonly ConcurrentQueue<DynamoDbStreamItem> ReceivedItems = new();

        public static IReadOnlyCollection<DynamoDbStreamItem> Items => ReceivedItems.ToArray();

        public static DynamoDbStreamRecordContext? LastContext { get; private set; }

        public static void Reset()
        {
            ReceivedItems.Clear();
            LastContext = null;
        }

        public ValueTask HandleAsync(
            DynamoDbStreamItem item,
            DynamoDbStreamRecordContext context,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastContext = context;

            if (context.EventName == "FAIL")
            {
                throw new InvalidOperationException("Expected test failure.");
            }

            ReceivedItems.Enqueue(item);
            return ValueTask.CompletedTask;
        }
    }
}