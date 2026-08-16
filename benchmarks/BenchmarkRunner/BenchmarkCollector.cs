using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace BenchmarkRunner;

internal static class BenchmarkCollector
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static async Task<int> CollectAsync(
        IReadOnlyList<BenchmarkSuite> suites,
        string? outputDirectory,
        bool allowDirty,
        CancellationToken cancellationToken)
    {
        var repositoryRoot = (await ProcessRunner.CaptureAsync(
            "git",
            ["rev-parse", "--show-toplevel"],
            Environment.CurrentDirectory,
            cancellationToken)).Trim();

        var benchmarkRoot = Path.Combine(repositoryRoot, "benchmarks");
        var benchmarkProject = Path.Combine(benchmarkRoot, "Benchmarks", "Benchmarks.csproj");
        var outputRoot = outputDirectory is null
            ? Path.Combine(benchmarkRoot, "results")
            : Path.GetFullPath(outputDirectory, Environment.CurrentDirectory);

        var commit = (await ProcessRunner.CaptureAsync(
            "git",
            ["rev-parse", "HEAD"],
            repositoryRoot,
            cancellationToken)).Trim();
        var shortCommit = (await ProcessRunner.CaptureAsync(
            "git",
            ["rev-parse", "--short=8", "HEAD"],
            repositoryRoot,
            cancellationToken)).Trim();
        var gitStatus = await ProcessRunner.CaptureAsync(
            "git",
            ["status", "--porcelain"],
            repositoryRoot,
            cancellationToken);
        var dirty = !string.IsNullOrWhiteSpace(gitStatus);

        if (dirty && !allowDirty)
        {
            Console.Error.WriteLine(
                "The working tree is dirty. Commit or stash changes before collecting benchmark results, or pass --allow-dirty explicitly.");
            return 2;
        }

        var sdkVersion = (await ProcessRunner.CaptureAsync(
            "dotnet",
            ["--version"],
            benchmarkRoot,
            cancellationToken)).Trim();
        var cpuModel = await MachineInfo.GetCpuModelAsync(cancellationToken);
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
            Suites: suites
                .Select(suite => new CollectionSuiteMetadata(suite.Id, suite.Filter, "pending", null))
                .ToArray());

        var collectionMetadataPath = Path.Combine(outputRoot, "metadata.json");
        var collectionReadmePath = Path.Combine(outputRoot, "README.md");
        await WriteJsonAsync(collectionMetadataPath, collectionMetadata, cancellationToken);
        await WriteCollectionReadmeAsync(collectionReadmePath, collectionMetadata, cancellationToken);

        var buildExitCode = await ProcessRunner.RunAsync(
            "dotnet",
            ["build", benchmarkProject, "--configuration", "Release"],
            benchmarkRoot,
            cancellationToken);

        if (buildExitCode != 0)
        {
            var failedCollection = collectionMetadata with { Status = "failed" };
            await WriteJsonAsync(collectionMetadataPath, failedCollection, cancellationToken);
            await WriteCollectionReadmeAsync(collectionReadmePath, failedCollection, cancellationToken);
            return buildExitCode;
        }

        for (var suiteIndex = 0; suiteIndex < suites.Count; suiteIndex++)
        {
            var result = await CollectSuiteAsync(
                suites[suiteIndex],
                suiteIndex,
                outputRoot,
                repositoryRoot,
                benchmarkProject,
                benchmarkRoot,
                timestamp,
                git,
                machine,
                dotNet,
                automation,
                collectionMetadata,
                collectionMetadataPath,
                collectionReadmePath,
                cancellationToken);

            collectionMetadata = result.CollectionMetadata;
            if (result.ExitCode != 0)
            {
                return result.ExitCode;
            }
        }

        collectionMetadata = collectionMetadata with { Status = "completed" };
        await WriteJsonAsync(collectionMetadataPath, collectionMetadata, cancellationToken);
        await WriteCollectionReadmeAsync(collectionReadmePath, collectionMetadata, cancellationToken);
        Console.WriteLine($"Benchmark collection completed: {GetDisplayPath(repositoryRoot, outputRoot)}");
        return 0;
    }

    private static async Task<SuiteCollectionResult> CollectSuiteAsync(
        BenchmarkSuite suite,
        int suiteIndex,
        string outputRoot,
        string repositoryRoot,
        string benchmarkProject,
        string benchmarkRoot,
        DateTimeOffset timestamp,
        GitMetadata git,
        MachineMetadata machine,
        DotNetMetadata dotNet,
        AutomationMetadata automation,
        BenchmarkCollectionMetadata collectionMetadata,
        string collectionMetadataPath,
        string collectionReadmePath,
        CancellationToken cancellationToken)
    {
        var runDirectory = CreateRunDirectory(outputRoot, suite.Id, timestamp, git.ShortCommit);
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
        await WriteJsonAsync(metadataPath, metadata, cancellationToken);
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
            benchmarkRoot,
            cancellationToken);

        var runPath = GetRunPath(outputRoot, runDirectory);
        if (benchmarkExitCode != 0)
        {
            await WriteJsonAsync(
                metadataPath,
                metadata with { Status = "failed", ExitCode = benchmarkExitCode },
                cancellationToken);
            var failedCollection = UpdateCollectionSuite(
                collectionMetadata,
                suiteIndex,
                "failed",
                runPath) with { Status = "failed" };
            await WriteJsonAsync(collectionMetadataPath, failedCollection, cancellationToken);
            await WriteCollectionReadmeAsync(collectionReadmePath, failedCollection, cancellationToken);
            return new SuiteCollectionResult(benchmarkExitCode, failedCollection);
        }

        var reports = FindReports(artifactsDirectory);
        if (reports.Count == 0)
        {
            const int missingReportExitCode = 3;
            Console.Error.WriteLine(
                $"Suite '{suite.Id}' completed successfully but produced no GitHub Markdown report.");
            await WriteJsonAsync(
                metadataPath,
                metadata with { Status = "failed", ExitCode = missingReportExitCode },
                cancellationToken);
            var failedCollection = UpdateCollectionSuite(
                collectionMetadata,
                suiteIndex,
                "failed",
                runPath) with { Status = "failed" };
            await WriteJsonAsync(collectionMetadataPath, failedCollection, cancellationToken);
            await WriteCollectionReadmeAsync(collectionReadmePath, failedCollection, cancellationToken);
            return new SuiteCollectionResult(missingReportExitCode, failedCollection);
        }

        var completedMetadata = metadata with { Status = "completed", ExitCode = 0 };
        await WriteJsonAsync(metadataPath, completedMetadata, cancellationToken);
        await WriteRunReadmeAsync(
            Path.Combine(runDirectory, "README.md"),
            completedMetadata,
            reports,
            cancellationToken);

        var completedCollection = UpdateCollectionSuite(
            collectionMetadata,
            suiteIndex,
            "completed",
            runPath);
        await WriteJsonAsync(collectionMetadataPath, completedCollection, cancellationToken);
        await WriteCollectionReadmeAsync(collectionReadmePath, completedCollection, cancellationToken);

        Console.WriteLine($"Benchmark run completed: {displayRunDirectory}");
        return new SuiteCollectionResult(0, completedCollection);
    }

    private static IReadOnlyList<string> FindReports(string artifactsDirectory)
    {
        var resultsDirectory = Path.Combine(artifactsDirectory, "results");
        return Directory.Exists(resultsDirectory)
            ? Directory
                .EnumerateFiles(resultsDirectory, "*-report-github.md", SearchOption.TopDirectoryOnly)
                .Order(StringComparer.Ordinal)
                .ToArray()
            : [];
    }

    private static BenchmarkCollectionMetadata UpdateCollectionSuite(
        BenchmarkCollectionMetadata metadata,
        int index,
        string status,
        string? runDirectory)
    {
        var suites = metadata.Suites.ToArray();
        suites[index] = suites[index] with { Status = status, RunDirectory = runDirectory };
        return metadata with { Suites = suites };
    }

    private static string CreateRunDirectory(
        string outputRoot,
        string suiteId,
        DateTimeOffset timestamp,
        string shortCommit)
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

    private static string GetRunPath(string outputRoot, string runDirectory) =>
        Path.GetRelativePath(outputRoot, runDirectory).Replace('\\', '/');

    private static string GetDisplayPath(string repositoryRoot, string path)
    {
        var relativePath = Path.GetRelativePath(repositoryRoot, path);
        if (!relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
            !string.Equals(relativePath, "..", StringComparison.Ordinal))
        {
            return relativePath.Replace('\\', '/');
        }

        return path;
    }

    private static async Task WriteJsonAsync<T>(
        string path,
        T value,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        await File.WriteAllTextAsync(path, json + Environment.NewLine, cancellationToken);
    }

    private static async Task WriteRunReadmeAsync(
        string path,
        BenchmarkRunMetadata metadata,
        IReadOnlyCollection<string> reports,
        CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# {metadata.Suite.Id} benchmark run");
        builder.AppendLine();
        AppendEnvironmentTable(
            builder,
            metadata.TimestampUtc,
            metadata.Git,
            metadata.Machine,
            metadata.DotNet,
            metadata.Automation);
        builder.AppendLine($"| Suite | `{metadata.Suite.Id}` |");
        builder.AppendLine($"| Filter | `{metadata.Suite.Filter}` |");

        foreach (var report in reports)
        {
            builder.AppendLine();
            builder.AppendLine("---");
            builder.AppendLine();
            builder.AppendLine(await File.ReadAllTextAsync(report, cancellationToken));
        }

        await File.WriteAllTextAsync(path, builder.ToString(), cancellationToken);
    }

    private static async Task WriteCollectionReadmeAsync(
        string path,
        BenchmarkCollectionMetadata metadata,
        CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Benchmark collection");
        builder.AppendLine();
        AppendEnvironmentTable(
            builder,
            metadata.TimestampUtc,
            metadata.Git,
            metadata.Machine,
            metadata.DotNet,
            metadata.Automation);
        builder.AppendLine($"| Status | {metadata.Status} |");
        builder.AppendLine();
        builder.AppendLine("## Suites");
        builder.AppendLine();
        builder.AppendLine("| Suite | Filter | Status | Report |");
        builder.AppendLine("|---|---|---|---|");

        foreach (var suite in metadata.Suites)
        {
            var report = suite.RunDirectory is null
                ? string.Empty
                : $"[open]({suite.RunDirectory}/README.md)";
            builder.AppendLine($"| `{suite.Id}` | `{suite.Filter}` | {suite.Status} | {report} |");
        }

        await File.WriteAllTextAsync(path, builder.ToString(), cancellationToken);
    }

    private static void AppendEnvironmentTable(
        StringBuilder builder,
        DateTimeOffset timestamp,
        GitMetadata git,
        MachineMetadata machine,
        DotNetMetadata dotNet,
        AutomationMetadata automation)
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

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0)
        {
            return "unknown";
        }

        const double gib = 1024d * 1024d * 1024d;
        return $"{bytes / gib:F1} GiB";
    }

    private sealed record SuiteCollectionResult(
        int ExitCode,
        BenchmarkCollectionMetadata CollectionMetadata);
}

internal static class ProcessRunner
{
    public static async Task<string> CaptureAsync(
        string fileName,
        IReadOnlyCollection<string> arguments,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        var startInfo = CreateStartInfo(fileName, arguments, workingDirectory);
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start {fileName}.");

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);
        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"{fileName} exited with code {process.ExitCode}:{Environment.NewLine}{error}");
        }

        return output;
    }

    public static async Task<int> RunAsync(
        string fileName,
        IReadOnlyCollection<string> arguments,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        using var process = Process.Start(CreateStartInfo(fileName, arguments, workingDirectory))
            ?? throw new InvalidOperationException($"Could not start {fileName}.");

        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode;
    }

    private static ProcessStartInfo CreateStartInfo(
        string fileName,
        IReadOnlyCollection<string> arguments,
        string workingDirectory)
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
    public static async Task<string?> GetCpuModelAsync(CancellationToken cancellationToken)
    {
        if (OperatingSystem.IsLinux())
        {
            const string cpuInfo = "/proc/cpuinfo";
            if (File.Exists(cpuInfo))
            {
                var lines = await File.ReadAllLinesAsync(cpuInfo, cancellationToken);
                var modelLine = lines.FirstOrDefault(
                    line => line.StartsWith("model name", StringComparison.OrdinalIgnoreCase));
                return modelLine?.Split(':', 2).ElementAtOrDefault(1)?.Trim();
            }
        }

        if (OperatingSystem.IsMacOS())
        {
            try
            {
                return (await ProcessRunner.CaptureAsync(
                    "sysctl",
                    ["-n", "machdep.cpu.brand_string"],
                    Environment.CurrentDirectory,
                    cancellationToken)).Trim();
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        return OperatingSystem.IsWindows()
            ? Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER")
            : null;
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

internal sealed record CollectionSuiteMetadata(
    string Id,
    string Filter,
    string Status,
    string? RunDirectory);

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
        if (!string.Equals(
                Environment.GetEnvironmentVariable("GITHUB_ACTIONS"),
                "true",
                StringComparison.OrdinalIgnoreCase))
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
