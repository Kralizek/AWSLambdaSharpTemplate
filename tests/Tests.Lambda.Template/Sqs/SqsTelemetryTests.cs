using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.SQSEvents;

using Kralizek.Lambda;

using NUnit.Framework;

namespace Tests.Lambda.Sqs;

[TestFixture]
public class SqsTelemetryTests
{
    [Test]
    public async Task Partial_batch_failure_marks_only_failed_record_and_preserves_invocation_status()
    {
        var activities = new ConcurrentBag<Activity>();
        var measurements = new ConcurrentBag<(long Value, string? Outcome)>();

        using var activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == LambdaTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activities.Add
        };
        ActivitySource.AddActivityListener(activityListener);

        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == LambdaTelemetry.MeterName &&
                instrument.Name == "kralizek.lambda.records")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
        {
            var outcome = tags.ToArray()
                .FirstOrDefault(tag => tag.Key == "kralizek.lambda.record.outcome")
                .Value as string;
            measurements.Add((value, outcome));
        });
        meterListener.Start();

        using var invocation = new Activity("lambda-invocation").Start();
        var sut = new PartialFailureSqsFunction();
        var input = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new() { MessageId = "first" },
                new()
                {
                    MessageId = "failed-message",
                    EventSourceArn = "arn:aws:sqs:eu-north-1:123456789012:orders"
                },
                new() { MessageId = "third" }
            }
        };

        var response = await sut.FunctionHandlerAsync(input, TestLambdaContexts.Create());

        var recordActivities = activities
            .Where(activity => activity.DisplayName == "record.process")
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(recordActivities, Has.Length.EqualTo(3));
            Assert.That(recordActivities.Count(activity => activity.Status == ActivityStatusCode.Error), Is.EqualTo(1));
            Assert.That(
                recordActivities.Single(activity => activity.Status == ActivityStatusCode.Error)
                    .GetTagItem("messaging.message.id"),
                Is.EqualTo("failed-message"));
            Assert.That(invocation.Status, Is.Not.EqualTo(ActivityStatusCode.Error));
            Assert.That(response.BatchItemFailures.Single().ItemIdentifier, Is.EqualTo("failed-message"));
            Assert.That(measurements.Count(measurement => measurement == (1L, "success")), Is.EqualTo(2));
            Assert.That(measurements.Count(measurement => measurement == (1L, "failure")), Is.EqualTo(1));
        });
    }

    private sealed class PartialFailureSqsFunction : SqsFunction<PartialFailureSqsHandler>;

    private sealed class PartialFailureSqsHandler : ISqsRecordHandler
    {
        public ValueTask<SqsRecordResult> HandleAsync(
            SQSEvent.SQSMessage record,
            SqsMessageContext context,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(
                record.MessageId == "failed-message"
                    ? SqsRecordResult.Failed("expected test failure")
                    : SqsRecordResult.Success);
    }
}
