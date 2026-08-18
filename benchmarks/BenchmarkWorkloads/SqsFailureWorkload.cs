using System.Threading.Tasks;

namespace BenchmarkWorkloads;

public enum SqsFailureMode
{
    ReturnedResult,
    Exception
}

public interface ISqsFailureTarget
{
    Task<int> InvokeAsync(int failurePercent, SqsFailureMode mode);
}

public sealed class SqsFailureBenchmarkMessage
{
    public string Message { get; set; } = string.Empty;

    public bool ShouldFail { get; set; }
}

public static class SqsFailureWorkload
{
    public const int BatchSize = 10;

    public static string CreateBody(bool shouldFail) =>
        shouldFail
            ? "{\"Message\":\"lambda benchmark\",\"ShouldFail\":true}"
            : "{\"Message\":\"lambda benchmark\",\"ShouldFail\":false}";

    public static bool ShouldFail(int recordIndex, int failurePercent) =>
        recordIndex < BatchSize * failurePercent / 100;

    public static string Execute(SqsFailureBenchmarkMessage message) =>
        message.Message.ToUpperInvariant();
}
