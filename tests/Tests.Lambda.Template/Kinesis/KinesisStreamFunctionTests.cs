using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.KinesisEvents;

using Kralizek.Lambda;

using NUnit.Framework;

namespace Tests.Lambda.Kinesis;

[TestFixture]
public class KinesisStreamFunctionTests
{
    [Test]
    public async Task Function_decodes_records_and_reports_failures_by_sequence_number()
    {
        TestHandler.Records.Clear();
        TestHandler.Contexts.Clear();
        var function = new TestFunction();
        var @event = new KinesisEvent
        {
            Records = new List<KinesisEvent.KinesisEventRecord>
            {
                CreateRecord("1", "{\"value\":\"ok\"}"),
                CreateRecord("2", "{\"value\":\"fail\"}")
            }
        };

        var response = await function.FunctionHandlerAsync(@event, TestLambdaContexts.Create());

        Assert.Multiple(() =>
        {
            Assert.That(TestHandler.Records, Is.EqualTo(new[] { "ok", "fail" }));
            Assert.That(response.BatchItemFailures.Select(failure => failure.ItemIdentifier), Is.EqualTo(new[] { "2" }));
        });
    }

    [Test]
    public async Task Handler_receives_Kinesis_record_metadata_and_raw_record()
    {
        TestHandler.Records.Clear();
        TestHandler.Contexts.Clear();
        var function = new TestFunction();
        var record = CreateRecord("42", "{\"value\":\"ok\"}");
        var @event = new KinesisEvent
        {
            Records = new List<KinesisEvent.KinesisEventRecord> { record }
        };

        await function.FunctionHandlerAsync(@event, TestLambdaContexts.Create());

        var context = TestHandler.Contexts.Single();

        Assert.Multiple(() =>
        {
            Assert.That(context.SequenceNumber, Is.EqualTo("42"));
            Assert.That(context.PartitionKey, Is.EqualTo("orders"));
            Assert.That(context.ApproximateArrivalTimestamp, Is.EqualTo(DateTime.UnixEpoch));
            Assert.That(context.GetKinesisRecord(), Is.SameAs(record));
        });
    }

    [Test]
    public void Failure_result_preserves_reason_and_union_case_value()
    {
        var result = KinesisStreamRecordResult.Failed("not ready");

        Assert.That(result.Value, Is.TypeOf<KinesisStreamRecordResult.FailureCase>());
        Assert.That(((KinesisStreamRecordResult.FailureCase)result.Value!).Reason, Is.EqualTo("not ready"));
    }

    private static KinesisEvent.KinesisEventRecord CreateRecord(string sequenceNumber, string payload) =>
        new()
        {
            EventId = $"shardId-000:{sequenceNumber}",
            Kinesis = new KinesisEvent.Record
            {
                SequenceNumber = sequenceNumber,
                PartitionKey = "orders",
                ApproximateArrivalTimestamp = DateTime.UnixEpoch,
                Data = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(payload))
            }
        };

    private sealed class TestFunction : KinesisStreamFunction<TestPayload, TestHandler>;

    private sealed class TestHandler : IKinesisStreamRecordHandler<TestPayload>
    {
        public static List<string> Records { get; } = new();

        public static List<KinesisStreamRecordContext> Contexts { get; } = new();

        public ValueTask<KinesisStreamRecordResult> HandleAsync(
            TestPayload payload,
            KinesisStreamRecordContext context,
            CancellationToken cancellationToken)
        {
            Records.Add(payload.Value);
            Contexts.Add(context);
            return ValueTask.FromResult(
                payload.Value == "fail"
                    ? KinesisStreamRecordResult.Failed("Expected test failure.")
                    : KinesisStreamRecordResult.Success);
        }
    }

    private sealed record TestPayload(string Value);
}