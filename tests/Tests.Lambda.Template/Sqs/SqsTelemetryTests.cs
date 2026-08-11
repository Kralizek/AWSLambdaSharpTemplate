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
    public async Task Failed_result_marks_record_activity_as_error_and_records_failure_metric()
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
        var sut = new FailedSqsFunction();
        var input = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new()
                {
                    MessageId = "failed-message",
                    EventSourceArn = "arn:aws:sqs:eu-north-1:123456789012:orders"
                }
            }
        };

        var response = await sut.FunctionHandlerAsync(input, TestLambdaContexts.Create());

        var recordActivity = activities.Single(activity => activity.DisplayName == "record.process");

        Assert.Multiple(() =>
        {
            Assert.That(recordActivity.Status, Is.EqualTo(ActivityStatusCode.Error));
            Assert.That(recordActivity.GetTagItem("messaging.message.id"), Is.EqualTo("failed-message"));
            Assert.That(response.BatchItemFailures.Single().ItemIdentifier, Is.EqualTo("failed-message"));
            Assert.That(measurements, Does.Contain((1L, "failure")));
        });
    }

    private sealed class FailedSqsFunction : SqsFunction<FailedSqsHandler>;

    private sealed class FailedSqsHandler : ISqsRecordHandler
    {
        public ValueTask<SqsRecordResult> HandleAsync(
            SQSEvent.SQSMessage record,
            SqsMessageContext context,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(SqsRecordResult.Failed("expected test failure"));
    }
}
