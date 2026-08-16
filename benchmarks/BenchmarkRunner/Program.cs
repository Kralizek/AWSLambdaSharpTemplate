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

var suites = ResolveSuites(options);
if (suites.Count == 0)
{
    Console.Error.WriteLine("Specify one or more benchmark suites, or use --all.");
    Console.Error.WriteLine($"Available suites: {string.Join(", ", BenchmarkSuites.All.Keys.Order())}");
    Console.Error.WriteLine("Use --list to show suite filters.");
    return 1;
}

var repositoryRoot = (await ProcessRunner.CaptureAsync("git", ["rev-parse", "--show-toplevel"], Environment.CurrentDirectory)).Trim();
var benchmarkRoot = Path.Combine(repositoryRoot, "benchmarks");
var benchmarkProject = Path.Combine(benchmarkRoot, "Benchmarks", "Benchmarks.csproj");
var outputRoot = options.OutputDirectory is null
    ? Path.Combine(benchmarkRoot, "results")
    : Path.GetFullPath(options.OutputDirectory, Environment.CurrentDirectory);

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
var machine = new MachineMetadata(
    Environment.MachineName,
    cpuModel,
    RuntimeInformation.OSDescription,
    RuntimeInformation.OSArchitecture.ToString(),
    RuntimeInformation.ProcessArchitecture.ToString(),
    Environment.ProcessorCount,
    GC.GetGCMemoryInfo().TotalAvailableMemoryBytes,
    Environment.GetEnvironmentVariable("BENCHMARK_POWER_MODE"));
var dotNet = new DotNetMetadata(sdkVersion, RuntimeInformation.FrameworkDescription);
var automation = AutomationMetadata.Create();
var git = new GitMetadata(commit, shortCommit, dirty);

Directory.CreateDirectory(outputRoot);

var collectionMetadata = new BenchmarkCollectionMetadata(
    SchemaVersion: 1,
    Status: "running",
    TimestampUtc: timestamp,
    Git: git,
    Machine: machine,
    DotNet: dotNet,
    Automation: automation,
    Suites: suites.Select(suite => new CollectionSuiteMetadata(suite.Id, suite.Filter, "pending", null)).ToArray());

var collectionMetadataPath = Path.Combine(outputRoot, "metadata.json");
await WriteJsonAsync(collectionMetadataPath, collectionMetadata);
await WriteCollectionReadmeAsync(Path.Combine(outputRoot, "README.md"), collectionMetadata);

var buildExitCode = await ProcessRunner.RunAsync(
    "dotnet",
    ["build", benchmarkProject, "--configuration", "Release"],
    benchmarkRoot);

if (buildExitCode != 0)
{
    var failedCollection = collectionMetadata with { Status = "failed" };
    await WriteJsonAsync(collectionMetadataPath, failedCollection);
    await WriteCollectionReadmeAsync(Path.Combine(outputRoot, "README.md"), failedCollection);
    return buildExitCode;
}

for (var suiteIndex = 0; suiteIndex < suites.Count; suiteIndex++)
{
    var suite = suites[suiteIndex];
    var runDirectory = CreateRunDirectory(outputRoot, suite.Id, timestamp, shortCommit);
    var artifactsDirectory = Path.Combine(runDirectory, "artifacts");
    Directory.CreateDirectory(artifactsDirectory);

    var displayRunDirectory = GetDisplayPath(repositoryRoot, runDirectory);
    var metadata = new BenchmarkRunMetadata(
        SchemaVersion: 1,
        Status: "running",
        Suite: new SuiteMetadata(suite.Id, suite.Filter),
        TimestampUtc: timestamp,
        Git: git,
        Machine: machine,
        DotNet: dotNet,
        Automation: automation,
        Benchmark: new BenchmarkMetadata(
            Configuration: "Release",
            Project: "benchmarks/Benchmarks/Benchmarks.csproj",
            Filter: suite.Filter,
            Exporters: ["GitHub", "CSV", "HTML"],
            ArtifactsDirectory: "artifacts"),
        ExitCode: null);

    var metadataPath = Path.Combine(runDirectory, "metadata.json");
    await WriteJsonAsync(metadataPath, metadata);
    Console.WriteLine($"Collecting suite '{suite.Id}' into {displayRunDirectory}");

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
        await WriteJsonAsync(metadataPath, metadata with { Status = "failed", ExitCode = benchmarkExitCode });
        collectionMetadata = UpdateCollectionSuite(collectionMetadata, suiteIndex, "failed", GetRunPath(outputRoot, runDirectory)) with { Status = "failed" };
        await WriteJsonAsync(collectionMetadataPath, collectionMetadata);
        await WriteCollectionReadmeAsync(Path.Combine(outputRoot, "README.md"), collectionMetadata);
        return benchmarkExitCode;
    }

    var resultsDirectory = Path.Combine(artifactsDirectory, "results");
    var reports = Directory.Exists(resultsDirectory)
        ? Directory.EnumerateFiles(resultsDirectory, "*-report-github.md", SearchOption.TopDirectoryOnly).Order(StringComparer.Ordinal).ToArray()
        : [];

    if (reports.Length == 0)
    {
        Console.Error.WriteLine($"Suite '{suite.Id}' completed successfully but produced no GitHub Markdown report.");
        await WriteJsonAsync(metadataPath, metadata with { Status = "failed", ExitCode = 3 });
        collectionMetadata = UpdateCollectionSuite(collectionMetadata, suiteIndex, "failed", GetRunPath(outputRoot, runDirectory)) with { Status = "failed" };
        await WriteJsonAsync(collectionMetadataPath, collectionMetadata);
        await WriteCollectionReadmeAsync(Path.Combine(outputRoot, "README.md"), collectionMetadata);
        return 3;
    }

    var completedMetadata = metadata with { Status = "completed", ExitCode = 0 };
    await WriteJsonAsync(metadataPath, completedMetadata);
    await WriteRunReadmeAsync(Path.Combine(runDirectory, "README.md"), completedMetadata, reports);

    collectionMetadata = UpdateCollectionSuite(collectionMetadata, suiteIndex, "completed", GetRunPath(outputRoot, runDirectory));
    await WriteJsonAsync(collectionMetadataPath, collectionMetadata);
    await WriteCollectionReadmeAsync(Path.Combine(outputRoot, "README.md"), collectionMetadata);

    Console.WriteLine($"Benchmark run completed: {displayRunDirectory}");
}

collectionMetadata = collectionMetadata with { Status = "completed" };
await WriteJsonAsync(collectionMetadataPath, collectionMetadata);
await WriteCollectionReadmeAsync(Path.Combine(outputRoot, "README.md"), collectionMetadata);
Console.WriteLine($"Benchmark collection completed: {GetDisplayPath(repositoryRoot, outputRoot)}");
return 0;

static IReadOnlyList<BenchmarkSuite> ResolveSuites(RunnerOptions options)
{
    if (options.AllSuites && options.SuiteIds.Count != 0)
    {
        throw new ArgumentException("--all cannot be combined with explicit suite names.");
    }

    if (options.AllSuites)
    {
        return BenchmarkSuites.All.Values.OrderBy(suite => suite.Id).ToArray();
    }

    var resolved = new List<BenchmarkSuite>();
    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var suiteId in options.SuiteIds)
    {
        if (!BenchmarkSuites.All.TryGetValue(suiteId, out var suite))
        {
            throw new ArgumentException($"Unknown benchmark suite: {suiteId}");
        }

        if (seen.Add(suite.Id))
        {
            resolved.Add(suite);
        }
    }

    return resolved;
}

static BenchmarkCollectionMetadata UpdateCollectionSuite(BenchmarkCollectionMetadata metadata, int index, string status, string? runDirectory)
{
    var suites = metadata.Suites.ToArray();
    suites[index] = suites[index] with { Status = status, RunDirectory = runDirectory };
    return metadata with { Suites = suites };
}

static string CreateRunDirectory(string outputRoot, string suiteId, DateTimeOffset timestamp, string shortCommit)
{
    var suiteDirectory = Path.Combine(outputRoot, suiteId);
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

static string GetRunPath(string outputRoot, string runDirectory) =>
    Path.GetRelativePath(outputRoot, runDirectory).Replace('\\', '/');

static string GetDisplayPath(string repositoryRoot, string path)
{
    var relativePath = Path.GetRelativePath(repositoryRoot, path);
    if (!relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) && relativePath != "..")
    {
        return relativePath.Replace('\\', '/');
    }

    return path;
}

static async Task WriteJsonAsync<T>(string path, T value)
{
    var json = JsonSerializer.Serialize(value, new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    });

    await File.WriteAllTextAsync(path, json + Environment.NewLine);
}

static async Task WriteRunReadmeAsync(string path, BenchmarkRunMetadata metadata, IReadOnlyCollection<string> reports)
{
    var builder = new StringBuilder();
    builder.AppendLine($"# {metadata.Suite.Id} benchmark run");
    builder.AppendLine();
    AppendEnvironmentTable(builder, metadata.TimestampUtc, metadata.Git, metadata.Machine, metadata.DotNet, metadata.Automation);
    builder.AppendLine($"| Suite | `{metadata.Suite.Id}` |");
    builder.AppendLine($"| Filter | `{metadata.Suite.Filter}` |");

    foreach (var report in reports)
    {
        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();
        builder.AppendLine(await File.ReadAllTextAsync(report));
    }

    await File.WriteAllTextAsync(path, builder.ToString());
}

static async Task WriteCollectionReadmeAsync(string path, BenchmarkCollectionMetadata metadata)
{
    var builder = new StringBuilder();
    builder.AppendLine("# Benchmark collection");
    builder.AppendLine();
    AppendEnvironmentTable(builder, metadata.TimestampUtc, metadata.Git, metadata.Machine, metadata.DotNet, metadata.Automation);
    builder.AppendLine($"| Status | {metadata.Status} |");
    builder.AppendLine();
    builder.AppendLine("## Suites");
    builder.AppendLine();
    builder.AppendLine("| Suite | Filter | Status | Report |");
    builder.AppendLine("|---|---|---|---|");

    foreach (var suite in metadata.Suites)
    {
        var report = suite.RunDirectory is null ? string.Empty : $"[open]({suite.RunDirectory}/README.md)";
        builder.AppendLine($"| `{suite.Id}` | `{suite.Filter}` | {suite.Status} | {report} |");
    }

    await File.WriteAllTextAsync(path, builder.ToString());
}

static void AppendEnvironmentTable(StringBuilder builder, DateTimeOffset timestamp, GitMetadata git, MachineMetadata machine, DotNetMetadata dotNet, AutomationMetadata automation)
{
    builder.AppendLine("| | |");
    builder.AppendLine("|---|---|");
    builder.AppendLine($"| Timestamp | `{timestamp:O}` |");
    builder.AppendLine($"| Commit | `{git.Commit}` |");
    builder.AppendLine($"| Git state | {(git.Dirty ? "dirty" : "clean")} |");
    builder.AppendLine($"| Machine | `{machine.Name}` |");
    builder.AppendLine($"| CPU | {machine.CpuModel ?? "unknown"} |");
    builder.AppendLine($"| OS | {machine.OperatingSystem} |");
    builder.AppendLine($"| Architecture | {machine.ProcessArchitecture} |");
    builder.AppendLine($"| Logical processors | {machine.LogicalProcessorCount} |");
    builder.AppendLine($"| Available memory | {FormatBytes(machine.TotalAvailableMemoryBytes)} |");
    builder.AppendLine($"| Power mode | {machine.PowerMode ?? "not recorded"} |");
    builder.AppendLine($"| .NET SDK | `{dotNet.SdkVersion}` |");
    builder.AppendLine($"| Execution | {automation.Provider} |");
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

internal sealed record RunnerOptions(
    IReadOnlyList<string> SuiteIds,
    bool AllSuites,
    bool AllowDirty,
    bool ListSuites,
    string? OutputDirectory)
{
    public static RunnerOptions Parse(string[] args)
    {
        var suiteIds = new List<string>();
        string? outputDirectory = null;
        var allSuites = false;
        var allowDirty = false;
        var listSuites = false;

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            switch (argument)
            {
                case "--all":
                    allSuites = true;
                    break;
                case "--allow-dirty":
                    allowDirty = true;
                    break;
                case "--list":
                    listSuites = true;
                    break;
                case "--output":
                    if (++index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
                    {
                        throw new ArgumentException("--output requires a directory.");
                    }

                    outputDirectory = args[index];
                    break;
                default when argument.StartsWith("--output=", StringComparison.Ordinal):
                    outputDirectory = argument["--output=".Length..];
                    if (string.IsNullOrWhiteSpace(outputDirectory))
                    {
                        throw new ArgumentException("--output requires a directory.");
                    }

                    break;
                default when argument.StartsWith('-'):
                    throw new ArgumentException($"Unknown option: {argument}");
                default:
                    suiteIds.Add(argument);
                    break;
            }
        }

        return new RunnerOptions(suiteIds, allSuites, allowDirty, listSuites, outputDirectory);
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

internal sealed record BenchmarkCollectionMetadata(
    int SchemaVersion,
    string Status,
    DateTimeOffset TimestampUtc,
    GitMetadata Git,
    MachineMetadata Machine,
    DotNetMetadata DotNet,
    AutomationMetadata Automation,
    IReadOnlyList<CollectionSuiteMetadata> Suites);

internal sealed record CollectionSuiteMetadata(string Id, string Filter, string Status, string? RunDirectory);

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
