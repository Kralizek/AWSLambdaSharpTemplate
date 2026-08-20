#nullable enable

using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;

using BenchmarkWorkloads;

namespace Benchmarks;

[MemoryDiagnoser]
public class NestedAsyncSqsSnsS3Benchmarks
{
    private TargetSession? _rawSdkSession;
    private TargetSession? _v5Session;
    private TargetSession? _v6Session;
    private ISqsTarget? _rawSdk;
    private ISqsTarget? _v5;
    private ISqsTarget? _v6Minimal;
    private ISqsTarget? _v6;

    [Params(1, 10)]
    public int BatchSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _rawSdkSession = TargetSession.Create("RawSdkTargetAssemblyPath");
        _v5Session = TargetSession.Create("V5TargetAssemblyPath");
        _v6Session = TargetSession.Create("V6TargetAssemblyPath");

        _rawSdk = _rawSdkSession.CreateTarget<ISqsTarget>("RawSdkTarget.NestedAsyncSqsSnsS3Target");
        _v5 = _v5Session.CreateTarget<ISqsTarget>("V5Target.NestedAsyncSqsSnsS3Target");
        _v6Minimal = _v6Session.CreateTarget<ISqsTarget>("V6Target.NestedAsyncMinimalSqsSnsS3Target");
        _v6 = _v6Session.CreateTarget<ISqsTarget>("V6Target.NestedAsyncSqsSnsS3Target");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _rawSdk = null;
        _v5 = null;
        _v6Minimal = null;
        _v6 = null;

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
    public Task<int> V5Async() => _v5!.InvokeAsync(BatchSize);

    [Benchmark]
    public Task<int> V6MinimalAsync() => _v6Minimal!.InvokeAsync(BatchSize);

    [Benchmark]
    public Task<int> V6Async() => _v6!.InvokeAsync(BatchSize);
}
