#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;

using BenchmarkWorkloads;

namespace Benchmarks;

[MemoryDiagnoser]
public class RequestBenchmarks
{
    private const string Input = "lambda benchmark";

    private readonly IRequestTarget _rawSdk = new RawSdkTarget.UppercaseTarget();
    private readonly IRequestTarget _v6 = new V6Target.UppercaseTarget();

    private V5TargetLoadContext? _v5LoadContext;
    private IRequestTarget? _v5;

    [GlobalSetup]
    public void Setup()
    {
        var v5TargetAssemblyPath = typeof(RequestBenchmarks).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "V5TargetAssemblyPath")
            .Value;

        if (string.IsNullOrWhiteSpace(v5TargetAssemblyPath) || !File.Exists(v5TargetAssemblyPath))
        {
            throw new FileNotFoundException("The V5 target assembly was not built.", v5TargetAssemblyPath);
        }

        _v5LoadContext = new V5TargetLoadContext(v5TargetAssemblyPath);
        var assembly = _v5LoadContext.LoadFromAssemblyPath(v5TargetAssemblyPath);
        var targetType = assembly.GetType("V5Target.UppercaseTarget", throwOnError: true)!;
        _v5 = (IRequestTarget)Activator.CreateInstance(targetType)!;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _v5 = null;
        _v5LoadContext?.Unload();
        _v5LoadContext = null;
    }

    [Benchmark(Baseline = true)]
    public Task<string> RawSdk() => _rawSdk.InvokeAsync(Input);

    [Benchmark]
    public Task<string> V5() => _v5!.InvokeAsync(Input);

    [Benchmark]
    public Task<string> V6() => _v6.InvokeAsync(Input);

    private sealed class V5TargetLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public V5TargetLoadContext(string targetAssemblyPath)
            : base(nameof(V5TargetLoadContext), isCollectible: true)
        {
            _resolver = new AssemblyDependencyResolver(targetAssemblyPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (assemblyName.Name == typeof(IRequestTarget).Assembly.GetName().Name)
            {
                return typeof(IRequestTarget).Assembly;
            }

            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            return assemblyPath is null ? null : LoadFromAssemblyPath(assemblyPath);
        }
    }
}
