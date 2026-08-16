using System.Threading.Tasks;

namespace BenchmarkWorkloads;

public interface IRequestTarget
{
    Task<string> InvokeAsync(string input);
}

public static class UppercaseWorkload
{
    public static string Execute(string input) => input.ToUpperInvariant();
}
