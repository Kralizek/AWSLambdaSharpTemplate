using System.Threading.Tasks;

namespace BenchmarkWorkloads;

public interface ISqsTarget
{
    Task<int> InvokeAsync(int batchSize);
}

public sealed class SqsBenchmarkMessage
{
    public string Message { get; set; } = string.Empty;
}

public static class SqsWorkload
{
    public const string Body = """{"Message":"lambda benchmark"}""";

    public static string Execute(SqsBenchmarkMessage message) => message.Message.ToUpperInvariant();
}
