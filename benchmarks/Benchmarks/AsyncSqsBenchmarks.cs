#nullable enable

using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;

using BenchmarkWorkloads;

namespace Benchmarks;

[MemoryDiagnoser]
public class AsyncSqsBenchmarks
{
    private TargetSession? _rawSdkSession;
    private TargetSession? _v5Session;
    private TargetSession? _v6Session;
    private ISqsTarget? _rawSdk;
    private ISqsTarget? _v5Typed;
    private ISqsTarget? _v6Raw;
    private ISqsTarget? _v6Typed;

    [Params(1, 10, 100)]
    public int BatchSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _rawSdkSession = TargetSession.Create("RawSdkTargetAssemblyPath");
        _v5Session = TargetSession.Create("V5TargetAssemblyPath");
        _v6Session = TargetSession.Create("V6TargetAssemblyPath");

        _rawSdk = _rawSdkSession.CreateTarget<ISqsTarget>("RawSdkTarget.UppercaseAsyncSqsTarget");
        _v5Typed = _v5Session.CreateTarget<ISqsTarget>("V5Target.UppercaseAsyncSqsTarget");
        _v6Raw = _v6Session.CreateTarget<ISqsTarget>("V6Target.UppercaseAsyncRawSqsTarget");
        _v6Typed = _v6Session.CreateTarget<ISqsTarget>("V6Target.UppercaseAsyncTypedSqsTarget");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _rawSdk = null;
        _v5Typed = null;
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
    public Task<int> RawSdkAsync() => _rawSdk!.InvokeAsync(BatchSize);

    [Benchmark]
    public Task<int> V5TypedAsync() => _v5Typed!.InvokeAsync(BatchSize);

    [Benchmark]
    public Task<int> V6RawAsync() => _v6Raw!.InvokeAsync(BatchSize);

    [Benchmark]
    public Task<int> V6TypedAsync() => _v6Typed!.InvokeAsync(BatchSize);
}
