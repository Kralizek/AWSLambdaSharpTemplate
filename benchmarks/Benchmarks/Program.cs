using System.Linq;

using BenchmarkDotNet.Running;

var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args).ToArray();

return summaries.Length == 0 ||
       summaries.Any(summary =>
           summary.ValidationErrors.Any(error => error.IsCritical) ||
           summary.Reports.Any(report => !report.Success))
    ? 1
    : 0;
