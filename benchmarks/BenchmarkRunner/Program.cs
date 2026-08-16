using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

using BenchmarkRunner;

var options = RunnerOptions.Parse(args);

if (options.ListSuites)
{
    foreach (var suite in BenchmarkSuites.All.Values.OrderBy(suite => suite.Id))
    {
        Console.WriteLine($"{suite.Id,-12} {suite.Filter}");
    }

    return 0;
}

if (options.SuiteId is null || !BenchmarkSuites.All.TryGetValue(options.SuiteId, out var suite))
{
    Console.Error.WriteLine("Specify a benchmark suite.");
    Console.Error.WriteLine($"Available suites: {string.Join(", ", BenchmarkSuites.All.Keys.Order())}");
    Console.Error.WriteLine("Use --list to show suite filters.");
    return 1;
}

var repositoryRoot = await ProcessRunner.CaptureAsync("git", ["rev-parse", "--show-toplevel"], Environment.CurrentDirectory);
repositoryRoot = repositoryRoot.Trim();

var benchmarkRoot = Path.Combine(repositoryRoot, "benchmarks");
var benchmarkProject = Path.Combine(benchmarkRoot, "Benchmarks", "Benchmarks.csproj");

var commit = (await ProcessRunner.CaptureAsync("git", ["rev-parse", "HEAD"], repositoryRoot)).Trim();
var shortCommit = (await ProcessRunner.CaptureAsync("git", ["rev-parse", "--short=8", "HEAD"], repositoryRoot)).Trim();
var gitStatus = await ProcessRunner.CaptureAsync("git", ["status", "--porcelain"], repositoryRoot);
var dirty = !string.IsNullOrWhiteSpace(gitStatus);

if (dirty && !options.AllowDirty)
{
    Console.Error.WriteLine("The working tree is dirty. Commit or stash changes before collecting benchmark results, or pass --allow-dirty explicitly.");
    return 2;
}

var sdkVersion = (await ProcessRunner.CaptureAsync("dotnet", ["--version"], benchmarkRoot)).Trim();
var cpuModel = await MachineInfo.GetCpuModelAsync();
var timestamp = DateTimeOffset.UtcNow;
var runDirectory = CreateRunDirectory(benchmarkRoot, suite.Id, timestamp, shortCommit);
var artifactsDirectory = Path.Combine(runDirectory, "artifacts");
Directory.CreateDirectory(artifactsDirectory);

var relativeRunDirectory = Path.GetRelativePath(repositoryRoot, runDirectory).Replace('\\', '/');
var relativeArtifactsDirectory = Path.GetRelativePath(repositoryRoot, artifactsDirectory).Replace('\\', '/');

var metadata = new BenchmarkRunMetadata(
    SchemaVersion: 1,
    Status: "running",
    Suite: new SuiteMetadata(suite.Id, suite.Filter),
    TimestampUtc: timestamp,
    Git: new GitMetadata(commit, shortCommit, dirty),
    Machine: new MachineMetadata(
        Environment.MachineName,
        cpuModel,
        RuntimeInformation.OSDescription,
        RuntimeInformation.OSArchitecture.ToString(),
        RuntimeInformation.ProcessArchitecture.ToString(),
        Environment.ProcessorCount,
        GC.GetGCMemoryInfo().TotalAvailableMemoryBytes,
        Environment.GetEnvironmentVariable("BENCHMARK_POWER_MODE")),
    DotNet: new DotNetMetadata(sdkVersion, RuntimeInformation.FrameworkDescription),
    Automation: AutomationMetadata.Create(),
    Benchmark: new BenchmarkMetadata(
        Configuration: "Release",
        Project: "benchmarks/Benchmarks/Benchmarks.csproj",
        Filter: suite.Filter,
        Exporters: ["GitHub", "CSV", "HTML"],
        ArtifactsDirectory: relativeArtifactsDirectory),
    ExitCode: null);

var metadataPath = Path.Combine(runDirectory, "metadata.json");
await WriteMetadataAsync(metadataPath, metadata);

Console.WriteLine($"Collecting suite '{suite.Id}' into {relativeRunDirectory}");

var buildExitCode = await ProcessRunner.RunAsync(
    "dotnet",
    ["build", benchmarkProject, "--configuration", "Release"],
    benchmarkRoot);

if (buildExitCode != 0)
{
    await WriteMetadataAsync(metadataPath, metadata with { Status = "failed", ExitCode = buildExitCode });
    return buildExitCode;
}

var benchmarkExitCode = await ProcessRunner.RunAsync(
    "dotnet",
    [
        "run",
        "--project", benchmarkProject,
        "--configuration", "Release",
        "--no-build",
        "--",
        "--filter", suite.Filter,
        "--artifacts", artifactsDirectory,
        "--exporters", "GitHub", "CSV", "HTML"
    ],
    benchmarkRoot);

if (benchmarkExitCode != 0)
{
    await WriteMetadataAsync(metadataPath, metadata with { Status = "failed", ExitCode = benchmarkExitCode });
    return benchmarkExitCode;
}

var reports = Directory
    .EnumerateFiles(Path.Combine(artifactsDirectory, "results"), "*-report-github.md", SearchOption.TopDirectoryOnly)
    .Order(StringComparer.Ordinal)
    .ToArray();

if (reports.Length == 0)
{
    Console.Error.WriteLine("BenchmarkDotNet completed successfully but produced no GitHub Markdown report.");
    await WriteMetadataAsync(metadataPath, metadata with { Status = "failed", ExitCode = 3 });
    return 3;
}

var completedMetadata = metadata with { Status = "completed", ExitCode = 0 };
await WriteMetadataAsync(metadataPath, completedMetadata);
await WriteReadmeAsync(Path.Combine(runDirectory, "README.md"), completedMetadata, reports);

Console.WriteLine($"Benchmark run completed: {relativeRunDirectory}");
return 0;

static string CreateRunDirectory(string benchmarkRoot, string suiteId, DateTimeOffset timestamp, string shortCommit)
{
    var suiteDirectory = Path.Combine(benchmarkRoot, "results", suiteId);
    Directory.CreateDirectory(suiteDirectory);

    var baseName = $"{timestamp:yyyy-MM-ddTHHmmssZ}-{shortCommit}";
    var candidate = Path.Combine(suiteDirectory, baseName);
    var suffix = 2;

    while (Directory.Exists(candidate))
    {
        candidate = Path.Combine(suiteDirectory, $"{baseName}-{suffix++}");
    }

    Directory.CreateDirectory(candidate);
    return candidate;
}

static async Task WriteMetadataAsync(string path, BenchmarkRunMetadata metadata)
{
    var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    });

    await File.WriteAllTextAsync(path, json + Environment.NewLine);
}

static async Task WriteReadmeAsync(string path, BenchmarkRunMetadata metadata, IReadOnlyCollection<string> reports)
{
    var builder = new StringBuilder();
    builder.AppendLine($"# {metadata.Suite.Id} benchmark run");
    builder.AppendLine();
    builder.AppendLine("| | |");
    builder.AppendLine("|---|---|");
    builder.AppendLine($"| Timestamp | `{metadata.TimestampUtc:O}` |");
    builder.AppendLine($"| Commit | `{metadata.Git.Commit}` |");
    builder.AppendLine($"| Git state | {(metadata.Git.Dirty ? "dirty" : "clean")} |");
    builder.AppendLine($"| Suite | `{metadata.Suite.Id}` |");
    builder.AppendLine($"| Filter | `{metadata.Suite.Filter}` |");
    builder.AppendLine($"| Machine | `{metadata.Machine.Name}` |");
    builder.AppendLine($"| CPU | {metadata.Machine.CpuModel ?? "unknown"} |");
    builder.AppendLine($"| OS | {metadata.Machine.OperatingSystem} |");
    builder.AppendLine($"| Architecture | {metadata.Machine.ProcessArchitecture} |");
    builder.AppendLine($"| Logical processors | {metadata.Machine.LogicalProcessorCount} |");
    builder.AppendLine($"| Available memory | {FormatBytes(metadata.Machine.TotalAvailableMemoryBytes)} |");
    builder.AppendLine($"| Power mode | {metadata.Machine.PowerMode ?? "not recorded"} |");
    builder.AppendLine($"| .NET SDK | `{metadata.DotNet.SdkVersion}` |");
    builder.AppendLine($"| Execution | {metadata.Automation.Provider} |");

    foreach (var report in reports)
    {
        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();
        builder.AppendLine(await File.ReadAllTextAsync(report));
    }

    await File.WriteAllTextAsync(path, builder.ToString());
}

static string FormatBytes(long bytes)
{
    if (bytes <= 0)
    {
        return "unknown";
    }

    const double gib = 1024d * 1024d * 1024d;
    return $"{bytes / gib:F1} GiB";
}

internal sealed record RunnerOptions(string? SuiteId, bool AllowDirty, bool ListSuites)
{
    public static RunnerOptions Parse(string[] args)
    {
        string? suiteId = null;
        var allowDirty = false;
        var listSuites = false;

        foreach (var argument in args)
        {
            switch (argument)
            {
                case "--allow-dirty":
                    allowDirty = true;
                    break;
                case "--list":
                    listSuites = true;
                    break;
                default when argument.StartsWith('-'):
                    throw new ArgumentException($"Unknown option: {argument}");
                default when suiteId is null:
                    suiteId = argument;
                    break;
                default:
                    throw new ArgumentException($"Unexpected argument: {argument}");
            }
        }

        return new RunnerOptions(suiteId, allowDirty, listSuites);
    }
}

internal static class ProcessRunner
{
    public static async Task<string> CaptureAsync(string fileName, IReadOnlyCollection<string> arguments, string workingDirectory)
    {
        var startInfo = CreateStartInfo(fileName, arguments, workingDirectory);
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start {fileName}.");

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();
        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"{fileName} exited with code {process.ExitCode}:{Environment.NewLine}{error}");
        }

        return output;
    }

    public static async Task<int> RunAsync(string fileName, IReadOnlyCollection<string> arguments, string workingDirectory)
    {
        using var process = Process.Start(CreateStartInfo(fileName, arguments, workingDirectory))
            ?? throw new InvalidOperationException($"Could not start {fileName}.");

        await process.WaitForExitAsync();
        return process.ExitCode;
    }

    private static ProcessStartInfo CreateStartInfo(string fileName, IReadOnlyCollection<string> arguments, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }
}

internal static class MachineInfo
{
    public static async Task<string?> GetCpuModelAsync()
    {
        if (OperatingSystem.IsLinux())
        {
            const string cpuInfo = "/proc/cpuinfo";
            if (File.Exists(cpuInfo))
            {
                var modelLine = (await File.ReadAllLinesAsync(cpuInfo))
                    .FirstOrDefault(line => line.StartsWith("model name", StringComparison.OrdinalIgnoreCase));
                return modelLine?.Split(':', 2).ElementAtOrDefault(1)?.Trim();
            }
        }

        if (OperatingSystem.IsMacOS())
        {
            try
            {
                return (await ProcessRunner.CaptureAsync("sysctl", ["-n", "machdep.cpu.brand_string"], Environment.CurrentDirectory)).Trim();
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        if (OperatingSystem.IsWindows())
        {
            return Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");
        }

        return null;
    }
}

internal sealed record BenchmarkRunMetadata(
    int SchemaVersion,
    string Status,
    SuiteMetadata Suite,
    DateTimeOffset TimestampUtc,
    GitMetadata Git,
    MachineMetadata Machine,
    DotNetMetadata DotNet,
    AutomationMetadata Automation,
    BenchmarkMetadata Benchmark,
    int? ExitCode);

internal sealed record SuiteMetadata(string Id, string Filter);
internal sealed record GitMetadata(string Commit, string ShortCommit, bool Dirty);
internal sealed record MachineMetadata(
    string Name,
    string? CpuModel,
    string OperatingSystem,
    string OsArchitecture,
    string ProcessArchitecture,
    int LogicalProcessorCount,
    long TotalAvailableMemoryBytes,
    string? PowerMode);
internal sealed record DotNetMetadata(string SdkVersion, string RuntimeDescription);
internal sealed record BenchmarkMetadata(
    string Configuration,
    string Project,
    string Filter,
    IReadOnlyCollection<string> Exporters,
    string ArtifactsDirectory);

internal sealed record AutomationMetadata(
    string Provider,
    string? Repository,
    string? RunId,
    string? RunAttempt,
    string? RunnerName,
    string? RunnerOs,
    string? RunnerArch,
    string? ImageOs,
    string? ImageVersion)
{
    public static AutomationMetadata Create()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return new AutomationMetadata("local", null, null, null, null, null, null, null, null);
        }

        return new AutomationMetadata(
            "github-actions",
            Environment.GetEnvironmentVariable("GITHUB_REPOSITORY"),
            Environment.GetEnvironmentVariable("GITHUB_RUN_ID"),
            Environment.GetEnvironmentVariable("GITHUB_RUN_ATTEMPT"),
            Environment.GetEnvironmentVariable("RUNNER_NAME"),
            Environment.GetEnvironmentVariable("RUNNER_OS"),
            Environment.GetEnvironmentVariable("RUNNER_ARCH"),
            Environment.GetEnvironmentVariable("ImageOS"),
            Environment.GetEnvironmentVariable("ImageVersion"));
    }
}
