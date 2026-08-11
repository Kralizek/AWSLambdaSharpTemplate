using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading.Tasks;

using Kralizek.Lambda;

using NUnit.Framework;

namespace Tests.Lambda;

[TestFixture]
public class TelemetryTests
{
    [Test]
    public async Task Request_function_enriches_current_activity_and_records_invocation()
    {
        var measurements = new ConcurrentBag<(long Value, string? Model)>();

        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == LambdaTelemetry.MeterName &&
                instrument.Name == "kralizek.lambda.invocations")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
        {
            var model = tags.ToArray()
                .FirstOrDefault(tag => tag.Key == "kralizek.lambda.function.model")
                .Value as string;
            measurements.Add((value, model));
        });
        meterListener.Start();

        using var invocation = new Activity("lambda-invocation").Start();
        var sut = new RequestFunctionTests.EchoHandlerFunction();

        await sut.FunctionHandlerAsync("hello", TestLambdaContexts.Create());

        Assert.Multiple(() =>
        {
            Assert.That(
                invocation.GetTagItem("kralizek.lambda.function.model"),
                Is.EqualTo("request"));
            Assert.That(measurements, Does.Contain((1L, "request")));
        });
    }

    [Test]
    public async Task Record_function_creates_child_activity_for_each_record_and_records_metrics()
    {
        var activities = new ConcurrentBag<Activity>();
        var recordMeasurements = new ConcurrentBag<(long Value, string? Outcome)>();
        var durationMeasurements = new ConcurrentBag<(double Value, string? Outcome)>();

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
            if (instrument.Meter.Name == LambdaTelemetry.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            if (instrument.Name == "kralizek.lambda.records")
            {
                recordMeasurements.Add((value, GetOutcome(tags)));
            }
        });
        meterListener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
        {
            if (instrument.Name == "kralizek.lambda.record.duration")
            {
                durationMeasurements.Add((value, GetOutcome(tags)));
            }
        });
        meterListener.Start();

        using var invocation = new Activity("lambda-invocation").Start();
        var sut = new RecordFunctionTests.SequentialRecordFunction();

        await sut.FunctionHandlerAsync(new[] { "a", "b" }, TestLambdaContexts.Create());

        var recordActivities = activities.Where(activity => activity.DisplayName == "record.process").ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(
                invocation.GetTagItem("kralizek.lambda.function.model"),
                Is.EqualTo("record"));
            Assert.That(recordActivities, Has.Length.EqualTo(2));
            Assert.That(recordActivities, Has.All.Property(nameof(Activity.ParentId)).EqualTo(invocation.Id));
            Assert.That(recordMeasurements.Count(measurement => measurement == (1L, "success")), Is.EqualTo(2));
            Assert.That(durationMeasurements.Count(measurement => measurement.Outcome == "success"), Is.EqualTo(2));
            Assert.That(durationMeasurements.All(measurement => measurement.Value >= 0), Is.True);
        });
    }

    private static string? GetOutcome(ReadOnlySpan<KeyValuePair<string, object?>> tags) =>
        tags.ToArray()
            .FirstOrDefault(tag => tag.Key == "kralizek.lambda.record.outcome")
            .Value as string;
}
