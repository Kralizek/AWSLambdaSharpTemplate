using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Kralizek.Lambda;

/// <summary>
/// Exposes the diagnostics instruments emitted by the Lambda programming model.
/// </summary>
public static class LambdaTelemetry
{
    /// <summary>
    /// The shared instrumentation name used by framework activities and metrics.
    /// </summary>
    public const string InstrumentationName = "Kralizek.Lambda.Template";

    /// <summary>
    /// The <see cref="ActivitySource"/> name to register with an OpenTelemetry tracer provider.
    /// </summary>
    public const string ActivitySourceName = InstrumentationName;

    /// <summary>
    /// The <see cref="Meter"/> name to register with an OpenTelemetry meter provider.
    /// </summary>
    public const string MeterName = InstrumentationName;

    /// <summary>
    /// The shared activity source used by the core programming model and AWS event specializations.
    /// </summary>
    public static ActivitySource ActivitySource { get; } = new(ActivitySourceName);

    /// <summary>
    /// The shared meter used by the core programming model and AWS event specializations.
    /// </summary>
    public static Meter Meter { get; } = new(MeterName);

    private static readonly Counter<long> InvocationCounter =
        Meter.CreateCounter<long>("kralizek.lambda.invocations", unit: "{invocation}");

    private static readonly Counter<long> RecordCounter =
        Meter.CreateCounter<long>("kralizek.lambda.records", unit: "{record}");

    private static readonly Histogram<double> RecordDuration =
        Meter.CreateHistogram<double>("kralizek.lambda.record.duration", unit: "s");

    internal static void EnrichInvocation(string functionModel)
    {
        Activity.Current?.SetTag("kralizek.lambda.function.model", functionModel);

        InvocationCounter.Add(
            1,
            new KeyValuePair<string, object?>("kralizek.lambda.function.model", functionModel));
    }

    internal static Activity? StartRecordActivity() =>
        ActivitySource.StartActivity("record.process", ActivityKind.Internal);

    internal static void RecordProcessed(string outcome, TimeSpan duration)
    {
        var outcomeTag = new KeyValuePair<string, object?>("kralizek.lambda.record.outcome", outcome);
        RecordCounter.Add(1, outcomeTag);
        RecordDuration.Record(duration.TotalSeconds, outcomeTag);
    }
}
