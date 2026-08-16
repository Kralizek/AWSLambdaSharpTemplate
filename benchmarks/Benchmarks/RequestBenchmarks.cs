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

    private TargetLoadContext? _v5LoadContext;
    private TargetLoadContext? _v6LoadContext;
    private IRequestTarget? _v5;
    private IRequestTarget? _v6;

    [GlobalSetup]
    public void Setup()
    {
        (_v5LoadContext, _v5) = LoadTarget("V5TargetAssemblyPath", "V5Target.UppercaseTarget");
        (_v6LoadContext, _v6) = LoadTarget("V6TargetAssemblyPath", "V6Target.UppercaseTarget");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _v5 = null;
        _v6 = null;

        _v5LoadContext?.Unload();
        _v6LoadContext?.Unload();

        _v5LoadContext = null;
        _v6LoadContext = null;
    }

    [Benchmark(Baseline = true)]
    public Task<string> RawSdk() => _rawSdk.InvokeAsync(Input);

    [Benchmark]
    public Task<string> V5() => _v5!.InvokeAsync(Input);

    [Benchmark]
    public Task<string> V6() => _v6!.InvokeAsync(Input);

    private static (TargetLoadContext LoadContext, IRequestTarget Target) LoadTarget(
        string metadataKey,
        string targetTypeName)
    {
        var targetAssemblyPath = typeof(RequestBenchmarks).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == metadataKey)
            .Value;

        if (string.IsNullOrWhiteSpace(targetAssemblyPath) || !File.Exists(targetAssemblyPath))
        {
            throw new FileNotFoundException($"The benchmark target assembly for '{metadataKey}' was not built.", targetAssemblyPath);
        }

        var loadContext = new TargetLoadContext(targetAssemblyPath, targetTypeName);
        var assembly = loadContext.LoadFromAssemblyPath(targetAssemblyPath);
        var targetType = assembly.GetType(targetTypeName, throwOnError: true)!;
        var target = (IRequestTarget)Activator.CreateInstance(targetType)!;

        return (loadContext, target);
    }

    private sealed class TargetLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public TargetLoadContext(string targetAssemblyPath, string name)
            : base(name, isCollectible: true)
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
