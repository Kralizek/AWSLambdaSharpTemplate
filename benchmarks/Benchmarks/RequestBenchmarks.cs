#nullable enable

using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;

using BenchmarkWorkloads;

namespace Benchmarks;

[MemoryDiagnoser]
public class RequestBenchmarks
{
    private const string Input = "lambda benchmark";

    private TargetSession? _rawSdkSession;
    private TargetSession? _v5Session;
    private TargetSession? _v6Session;
    private IRequestTarget? _rawSdk;
    private IRequestTarget? _v5;
    private IRequestTarget? _v6;

    [GlobalSetup]
    public void Setup()
    {
        _rawSdkSession = TargetSession.Create("RawSdkTargetAssemblyPath");
        _v5Session = TargetSession.Create("V5TargetAssemblyPath");
        _v6Session = TargetSession.Create("V6TargetAssemblyPath");

        _rawSdk = _rawSdkSession.CreateTarget<IRequestTarget>("RawSdkTarget.UppercaseTarget");
        _v5 = _v5Session.CreateTarget<IRequestTarget>("V5Target.UppercaseTarget");
        _v6 = _v6Session.CreateTarget<IRequestTarget>("V6Target.UppercaseTarget");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _rawSdk = null;
        _v5 = null;
        _v6 = null;

        _rawSdkSession?.Dispose();
        _v5Session?.Dispose();
        _v6Session?.Dispose();

        _rawSdkSession = null;
        _v5Session = null;
        _v6Session = null;
    }

    [Benchmark(Baseline = true)]
    public Task<string> RawSdk() => _rawSdk!.InvokeAsync(Input);

    [Benchmark]
    public Task<string> V5() => _v5!.InvokeAsync(Input);

    [Benchmark]
    public Task<string> V6() => _v6!.InvokeAsync(Input);
}
