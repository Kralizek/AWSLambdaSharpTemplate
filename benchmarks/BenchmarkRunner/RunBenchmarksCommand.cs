using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Spectre.Console;
using Spectre.Console.Cli;

namespace BenchmarkRunner;

internal sealed class RunBenchmarksCommand : AsyncCommand<RunBenchmarksCommand.Settings>
{
    internal sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[SUITES]")]
        [Description("One or more benchmark suite identifiers to run.")]
        public string[] SuiteIds { get; init; } = [];

        [CommandOption("--all")]
        [Description("Run every registered benchmark suite.")]
        public bool AllSuites { get; init; }

        [CommandOption("--list")]
        [Description("List the registered suites and their BenchmarkDotNet filters.")]
        public bool ListSuites { get; init; }

        [CommandOption("--allow-dirty")]
        [Description("Allow collection from a dirty Git working tree.")]
        public bool AllowDirty { get; init; }

        [CommandOption("-o|--output <DIRECTORY>")]
        [Description("Root directory where benchmark results are collected.")]
        public string? OutputDirectory { get; init; }

        public override ValidationResult Validate()
        {
            if (ListSuites)
            {
                return AllSuites || SuiteIds.Length != 0
                    ? ValidationResult.Error("--list cannot be combined with suite names or --all.")
                    : ValidationResult.Success();
            }

            if (AllSuites && SuiteIds.Length != 0)
            {
                return ValidationResult.Error("--all cannot be combined with explicit suite names.");
            }

            if (!AllSuites && SuiteIds.Length == 0)
            {
                return ValidationResult.Error("Specify one or more benchmark suites, or use --all.");
            }

            var unknownSuite = SuiteIds.FirstOrDefault(suiteId => !BenchmarkSuites.All.ContainsKey(suiteId));
            if (unknownSuite is not null)
            {
                return ValidationResult.Error($"Unknown benchmark suite: {unknownSuite}");
            }

            return ValidationResult.Success();
        }
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken)
    {
        if (settings.ListSuites)
        {
            foreach (var suite in BenchmarkSuites.All.Values.OrderBy(suite => suite.Id))
            {
                await Console.Out.WriteLineAsync($"{suite.Id,-12} {suite.Filter}");
            }

            return 0;
        }

        var suites = settings.AllSuites
            ? BenchmarkSuites.All.Values.OrderBy(suite => suite.Id).ToArray()
            : settings.SuiteIds
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(suiteId => BenchmarkSuites.All[suiteId])
                .ToArray();

        try
        {
            return await BenchmarkCollector.CollectAsync(
                suites,
                settings.OutputDirectory,
                settings.AllowDirty,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await Console.Error.WriteLineAsync("Benchmark collection cancelled.");
            return 130;
        }
        catch (Exception exception)
        {
            await Console.Error.WriteLineAsync($"Benchmark collection failed: {exception.Message}");
            return 1;
        }
    }
}
