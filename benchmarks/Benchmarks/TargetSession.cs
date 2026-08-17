#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

using BenchmarkWorkloads;

namespace Benchmarks;

internal sealed class TargetSession : IDisposable
{
    private readonly TargetLoadContext _loadContext;
    private readonly string _targetAssemblyPath;

    private TargetSession(string targetAssemblyPath, string name)
    {
        _targetAssemblyPath = targetAssemblyPath;
        _loadContext = new TargetLoadContext(targetAssemblyPath, name);
    }

    public static TargetSession Create(string metadataKey)
    {
        var targetAssemblyPath = typeof(TargetSession).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == metadataKey)
            .Value;

        if (string.IsNullOrWhiteSpace(targetAssemblyPath) || !File.Exists(targetAssemblyPath))
        {
            throw new FileNotFoundException($"The benchmark target assembly for '{metadataKey}' was not built.", targetAssemblyPath);
        }

        return new TargetSession(targetAssemblyPath, metadataKey);
    }

    public TContract CreateTarget<TContract>(string targetTypeName)
        where TContract : class
    {
        var assembly = _loadContext.LoadFromAssemblyPath(_targetAssemblyPath);
        var targetType = assembly.GetType(targetTypeName, throwOnError: true)!;
        return (TContract)Activator.CreateInstance(targetType)!;
    }

    public void Dispose() => _loadContext.Unload();

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
