#nullable enable

using System;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;

using BenchmarkWorkloads;

namespace Benchmarks;

[MemoryDiagnoser]
public class SqsReturnedFailureBenchmarks
{
    private TargetSession? _rawSdkSession;
    private TargetSession? _v5Session;
    private TargetSession? _v6Session;
    private ISqsFailureTarget? _rawSdk;
    private ISqsFailureTarget? _v5;
    private ISqsFailureTarget? _v6Raw;
    private ISqsFailureTarget? _v6Typed;

    [Params(0, 10, 50, 100)]
    public int FailurePercent { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _rawSdkSession = TargetSession.Create("RawSdkTargetAssemblyPath");
        _v5Session = TargetSession.Create("V5TargetAssemblyPath");
        _v6Session = TargetSession.Create("V6TargetAssemblyPath");

        _rawSdk = _rawSdkSession.CreateTarget<ISqsFailureTarget>("RawSdkTarget.FailureSqsTarget");
        _v5 = _v5Session.CreateTarget<ISqsFailureTarget>("V5Target.FailureSqsTarget");
        _v6Raw = _v6Session.CreateTarget<ISqsFailureTarget>("V6Target.FailureRawSqsTarget");
        _v6Typed = _v6Session.CreateTarget<ISqsFailureTarget>("V6Target.FailureTypedSqsTarget");

        ValidateTarget(_rawSdk, "RawSdk");
        ValidateTarget(_v5, "V5RequestResponse");
        ValidateTarget(_v6Raw, "V6Raw");
        ValidateTarget(_v6Typed, "V6Typed");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _rawSdk = null;
        _v5 = null;
        _v6Raw = null;
        _v6Typed = null;

        _rawSdkSession?.Dispose();
        _v5Session?.Dispose();
        _v6Session?.Dispose();

        _rawSdkSession = null;
        _v5Session = null;
        _v6Session = null;
    }

    [Benchmark(Baseline = true)]
    public Task<int> RawSdkReturnedFailure() =>
        _rawSdk!.InvokeAsync(FailurePercent, SqsFailureMode.ReturnedResult);

    [Benchmark]
    public Task<int> V5RequestResponseReturnedFailure() =>
        _v5!.InvokeAsync(FailurePercent, SqsFailureMode.ReturnedResult);

    [Benchmark]
    public Task<int> V6RawReturnedFailure() =>
        _v6Raw!.InvokeAsync(FailurePercent, SqsFailureMode.ReturnedResult);

    [Benchmark]
    public Task<int> V6TypedReturnedFailure() =>
        _v6Typed!.InvokeAsync(FailurePercent, SqsFailureMode.ReturnedResult);

    private void ValidateTarget(ISqsFailureTarget target, string targetName)
    {
        var actual = target.InvokeAsync(FailurePercent, SqsFailureMode.ReturnedResult).GetAwaiter().GetResult();
        var expected = SqsFailureWorkload.BatchSize * FailurePercent / 100;

        if (actual != expected)
        {
            throw new InvalidOperationException(
                $"{targetName} returned {actual} failed records; expected {expected} for {FailurePercent}% failures.");
        }
    }
}
