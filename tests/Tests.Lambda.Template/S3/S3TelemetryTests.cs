using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Kralizek.Lambda;

using NUnit.Framework;

namespace Tests.Lambda.S3;

[TestFixture]
public class S3TelemetryTests
{
    [Test]
    public async Task Batch_result_cases_drive_span_status_and_record_metrics()
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
        var input = new S3BatchEvent
        {
            InvocationSchemaVersion = "2.0",
            InvocationId = "invocation-1",
            Tasks = new List<S3BatchTask>
            {
                CreateTask("succeeded"),
                CreateTask("temporary"),
                CreateTask("permanent")
            }
        };

        var response = await new BatchFunction().FunctionHandlerAsync(input, TestLambdaContexts.Create());

        var recordActivities = activities
            .Where(activity => activity.DisplayName == "record.process")
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(recordActivities, Has.Length.EqualTo(3));
            Assert.That(
                recordActivities.Single(activity => activity.GetTagItem("kralizek.aws.s3.batch.task_id") as string == "succeeded").Status,
                Is.Not.EqualTo(ActivityStatusCode.Error));
            Assert.That(
                recordActivities.Single(activity => activity.GetTagItem("kralizek.aws.s3.batch.task_id") as string == "temporary").Status,
                Is.EqualTo(ActivityStatusCode.Error));
            Assert.That(
                recordActivities.Single(activity => activity.GetTagItem("kralizek.aws.s3.batch.task_id") as string == "permanent").Status,
                Is.EqualTo(ActivityStatusCode.Error));
            Assert.That(
                recordActivities.Select(activity => activity.GetTagItem("kralizek.aws.s3.batch.result")),
                Is.EquivalentTo(new object[] { "succeeded", "temporary_failure", "permanent_failure" }));
            Assert.That(measurements.Count(measurement => measurement == (1L, "success")), Is.EqualTo(1));
            Assert.That(measurements.Count(measurement => measurement == (1L, "failure")), Is.EqualTo(2));
            Assert.That(
                response.Results.Select(result => result.ResultCode),
                Is.EquivalentTo(new[] { "Succeeded", "TemporaryFailure", "PermanentFailure" }));
        });
    }

    private static S3BatchTask CreateTask(string taskId) =>
        new()
        {
            TaskId = taskId,
            S3Bucket = "uploads",
            S3Key = $"{taskId}.txt"
        };

    private sealed class BatchFunction : S3BatchFunction<BatchHandler>;

    private sealed class BatchHandler : IS3BatchItemHandler
    {
        public ValueTask<S3BatchResult> HandleAsync(
            S3BatchItem item,
            S3BatchContext context,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(
                context.TaskId switch
                {
                    "temporary" => S3BatchResult.TemporaryFailure(),
                    "permanent" => S3BatchResult.PermanentFailure(),
                    _ => S3BatchResult.Succeeded()
                });
    }
}
