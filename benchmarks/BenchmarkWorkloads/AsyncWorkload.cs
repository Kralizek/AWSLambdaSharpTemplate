using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BenchmarkWorkloads;

public static class AsyncWorkload
{
    public static YieldAwaitable Suspend() => Task.Yield();
}
