using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.TestUtilities;
using Kralizek.Lambda;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Tests.Lambda.Sqs;

public class SqsEventHandlerDisposalTests
{
    [Test]
    public async Task EventHandler_Should_Use_Scoped_Object_In_ForEach_Loop()
    {
        var sqsEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new SQSEvent.SQSMessage
                {
                    Body = "{}"
                },
                new SQSEvent.SQSMessage
                {
                    Body = "{}"
                },
            }
        };

        var dependency = new DisposableDependency();

        var services = new ServiceCollection();

        services.AddScoped(_ => dependency);

        var tcs = new TaskCompletionSource<TestMessage>();
        services.AddTransient<IEventHandler<SQSEvent>, SqsEventHandler<TestMessage>>();

        services.AddTransient<IMessageHandler<TestMessage>,
            TestMessageScopedHandler>(provider =>
            new TestMessageScopedHandler(provider.GetRequiredService<DisposableDependency>(), tcs));

        services.AddSingleton<IMessageSerializer, DefaultJsonMessageSerializer>();

        var sp = services.BuildServiceProvider();
        var sqsEventHandler = new SqsEventHandler<TestMessage>(sp, new NullLoggerFactory());

        var task = sqsEventHandler.HandleAsync(sqsEvent, new TestLambdaContext());

        Assert.That(dependency.Disposed, Is.False, "Dependency should not be disposed");
        Assert.That(task.IsCompleted, Is.False, "The task should not be completed");

        tcs.SetResult(new TestMessage());
        await task;
        Assert.That(dependency.Disposed, Is.True, "Dependency should be disposed");
        Assert.That(task.IsCompleted, Is.True, "The task should be completed");


    }

    [Test]
    public async Task EventHandler_Should_Use_Scoped_Object_In_ForEachAsync_Loop()
    {
        var sqsEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new SQSEvent.SQSMessage
                {
                    Body = "{}"
                },
                new SQSEvent.SQSMessage
                {
                    Body = "{}"
                },
            }
        };

        var dependency = new DisposableDependency();

        var services = new ServiceCollection();

        services.AddScoped(_ => dependency);

        var tcs = new TaskCompletionSource<TestMessage>();
        services.AddTransient<IEventHandler<SQSEvent>, ParallelSqsEventHandler<TestMessage>>();

        services.AddTransient<IMessageHandler<TestMessage>,
            TestMessageScopedHandler>(provider =>
            new TestMessageScopedHandler(provider.GetRequiredService<DisposableDependency>(), tcs));
            
        services.AddSingleton<IMessageSerializer, DefaultJsonMessageSerializer>();

        var sp = services.BuildServiceProvider();
        var sqsEventHandler = new ParallelSqsEventHandler<TestMessage>(sp, new NullLoggerFactory(), Options.Create(new ParallelSqsExecutionOptions{MaxDegreeOfParallelism = 4}));

        var task = sqsEventHandler.HandleAsync(sqsEvent, new TestLambdaContext());

        Assert.That(dependency.Disposed, Is.False, "Dependency should not be disposed");
        Assert.That(task.IsCompleted, Is.False, "The task should not be completed");

        tcs.SetResult(new TestMessage());
        await task;
        Assert.That(dependency.Disposed, Is.True, "Dependency should be disposed");
        Assert.That(task.IsCompleted, Is.True, "The task should be completed");
    }

    private class DisposableDependency : IDisposable
    {
        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
    }

    private class TestMessageScopedHandler : IMessageHandler<TestMessage>
    {
        private readonly DisposableDependency _dependency;
        private readonly TaskCompletionSource<TestMessage> _tcs;

        public TestMessageScopedHandler(DisposableDependency dependency, TaskCompletionSource<TestMessage> tcs)
        {
            _dependency = dependency;
            _tcs = tcs;
        }

        public Task HandleAsync(TestMessage message, ILambdaContext context) => _tcs.Task;
    }
}