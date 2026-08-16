using BenchmarkRunner;

using Spectre.Console.Cli;

using var cancellationTokenSource = new CancellationTokenSource();

Console.CancelKeyPress += OnCancelKeyPress;

try
{
    var app = new CommandApp<RunBenchmarksCommand>();
    app.Configure(config =>
    {
        config.SetApplicationName("benchmark-runner");
        config.SetApplicationVersion("1");
    });

    return await app.RunAsync(args, cancellationTokenSource.Token);
}
finally
{
    Console.CancelKeyPress -= OnCancelKeyPress;
}

void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs eventArgs)
{
    eventArgs.Cancel = true;
    cancellationTokenSource.Cancel();
}
