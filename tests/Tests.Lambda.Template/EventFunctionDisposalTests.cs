using System;
using System.Threading;
using System.Threading.Tasks;

using Kralizek.Lambda;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

namespace Tests.Lambda;

[TestFixture]
public class EventFunctionDisposalTests
{
    [Test]
    public void FunctionHandlerAsync_is_awaited_before_scope_disposal()
    {
        PendingHandler.Tcs = new TaskCompletionSource<bool>();
        PendingHandler.Dependency = new DisposableDependency();

        var sut = new TestEventFunction();
        var task = sut.FunctionHandlerAsync("trigger", TestLambdaContexts.Create());

        Assert.That(PendingHandler.Dependency.Disposed, Is.False, "Dependency should not be disposed before handler completes");
        Assert.That(task.IsCompleted, Is.False, "Task should still be in-flight");

        PendingHandler.Tcs.SetResult(true);

        Assert.That(PendingHandler.Dependency.Disposed, Is.True, "Dependency should be disposed after handler completes");
        Assert.That(task.IsCompleted, Is.True, "Task should be completed");
    }

    public class TestEventFunction : EventFunction<string, PendingHandler>
    {
        protected override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped(_ => PendingHandler.Dependency!);
        }
    }

    public class DisposableDependency : IDisposable
    {
        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
    }

    public class PendingHandler : IEventHandler<string>
    {
        public static TaskCompletionSource<bool>? Tcs { get; set; }
        public static DisposableDependency? Dependency { get; set; }

        private readonly DisposableDependency _dependency;

        public PendingHandler(DisposableDependency dependency)
        {
            _dependency = dependency;
        }

        public async ValueTask HandleAsync(string input, EventContext context, CancellationToken cancellationToken)
            => await Tcs!.Task.ConfigureAwait(false);
    }
}