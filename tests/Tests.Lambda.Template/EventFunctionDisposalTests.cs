using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using Kralizek.Lambda;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Tests.Lambda
{
    public class EventFunctionDisposalTests
    {
        [Test]
        public void FunctionHandlerAsync_is_awaited_before_disposal()
        {
            var dependency = new DisposableDependency();
            var taskCompletionSource = new TaskCompletionSource<string>();
            var sut = new TestEventFunction(dependency, taskCompletionSource);

            var result = sut.FunctionHandlerAsync("Hi there", new TestLambdaContext());
            
            Assert.That(dependency.Disposed, Is.False, "Dependency should not be disposed");
            Assert.That(result.IsCompleted, Is.False, "The task should not be completed");

            taskCompletionSource.SetResult("done");
            
            Assert.That(dependency.Disposed, Is.True, "Dependency should be disposed");
            Assert.That(result.IsCompleted, Is.True, "The task should be completed");
        }

        public class TestEventFunction : EventFunction<string>
        {
            private readonly DisposableDependency _dependency;
            private readonly TaskCompletionSource<string> _tcs;

            public TestEventFunction(DisposableDependency dependency, TaskCompletionSource<string> tcs)
            {
                _dependency = dependency;
                _tcs = tcs;
            }

            protected override void ConfigureServices(IServiceCollection services, IExecutionEnvironment executionEnvironment)
            {
                services.AddScoped(_ => _dependency);
                services.AddTransient<IEventHandler<string>>(s => new TestHandler(s.GetRequiredService<DisposableDependency>(), _tcs));
            }
        }
        
        public class DisposableDependency : IDisposable
        {
            public bool Disposed { get; private set; }
            public void Dispose() => Disposed = true;
        }

        private class TestHandler : IEventHandler<string>
        {
            // ReSharper disable once NotAccessedField.Local
            private readonly DisposableDependency _dependency;
            private readonly TaskCompletionSource<string> _tcs;

            public TestHandler(DisposableDependency dependency, TaskCompletionSource<string> tcs)
            {
                _dependency = dependency;
                _tcs = tcs;
            }

            public Task HandleAsync(string input, ILambdaContext context) => _tcs.Task;
        }
    }
}