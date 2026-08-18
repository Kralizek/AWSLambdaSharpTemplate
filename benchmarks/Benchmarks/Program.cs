using System;
using System.Collections.Generic;
using System.Linq;

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

using Benchmarks;

var (profile, benchmarkArgs) = ParseProfile(args);
var isListCommand = benchmarkArgs.Any(argument =>
    string.Equals(argument, "--list", StringComparison.OrdinalIgnoreCase) ||
    argument.StartsWith("--list=", StringComparison.OrdinalIgnoreCase));

var config = ManualConfig.Create(DefaultConfig.Instance)
    .AddFilter(BenchmarkProfiles.CreateFilter(profile));

var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
    .Run(benchmarkArgs, config)
    .ToArray();

return (!isListCommand && summaries.Length == 0) ||
       summaries.Any(summary =>
           summary.ValidationErrors.Any(error => error.IsCritical) ||
           summary.Reports.Any(report => !report.Success))
    ? 1
    : 0;

static (BenchmarkProfile Profile, string[] Args) ParseProfile(string[] args)
{
    var profile = BenchmarkProfile.Full;
    var remainingArgs = new List<string>(args.Length);
    var index = 0;

    while (index < args.Length)
    {
        var argument = args[index];

        if (argument.StartsWith("--profile=", StringComparison.OrdinalIgnoreCase))
        {
            profile = BenchmarkProfiles.Parse(argument["--profile=".Length..]);
            index++;
            continue;
        }

        if (string.Equals(argument, "--profile", StringComparison.OrdinalIgnoreCase))
        {
            index++;

            if (index >= args.Length)
            {
                throw new ArgumentException("--profile requires a value: full, ci, or stress.");
            }

            profile = BenchmarkProfiles.Parse(args[index]);
            index++;
            continue;
        }

        remainingArgs.Add(argument);
        index++;
    }

    return (profile, remainingArgs.ToArray());
}
