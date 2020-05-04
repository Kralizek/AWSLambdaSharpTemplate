using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.TestUtilities;
using Kralizek.Lambda;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Tests.Lambda.Sns
{
    public class SnsEventHandlerDisposalTests
    {
        [Test]
        public async Task EventHandler_Should_Use_Scoped_Object_In_ForEach_Loop()
        {
            var snsEvent = new SNSEvent
            {
                Records = new List<SNSEvent.SNSRecord>
                {
                    new SNSEvent.SNSRecord
                    {
                        Sns = new SNSEvent.SNSMessage
                        {
                            Message = "{}"
                        }
                    },
                    new SNSEvent.SNSRecord
                    {
                        Sns = new SNSEvent.SNSMessage
                        {
                            Message = "{}"
                        }
                    }
                }
            };

            var dependency = new DisposableDependency();

            var services = new ServiceCollection();

            services.AddScoped(_ => dependency);

            var tcs = new TaskCompletionSource<TestNotification>();
            services.AddTransient<IEventHandler<SQSEvent>, SqsEventHandler<TestNotification>>();

            services.AddTransient<INotificationHandler<TestNotification>,
                TestNotificationScopedHandler>(provider =>
                new TestNotificationScopedHandler(provider.GetRequiredService<DisposableDependency>(), tcs));

            var sp = services.BuildServiceProvider();
            var snsEventHandler = new SnsEventHandler<TestNotification>(sp, new NullLoggerFactory());

            var task = snsEventHandler.HandleAsync(snsEvent, new TestLambdaContext());

            Assert.That(dependency.Disposed, Is.False, "Dependency should not be disposed");
            Assert.That(task.IsCompleted, Is.False, "The task should not be completed");

            tcs.SetResult(new TestNotification());

            await task;

            Assert.That(dependency.Disposed, Is.True, "Dependency should be disposed");
            Assert.That(task.IsCompleted, Is.True, "The task should be completed");
        }

        private class DisposableDependency : IDisposable
        {
            public bool Disposed { get; private set; }
            public void Dispose() => Disposed = true;
        }

        private class TestNotificationScopedHandler: INotificationHandler<TestNotification>
        {
            private readonly DisposableDependency _dependency;
            private readonly TaskCompletionSource<TestNotification> _tcs;

            public TestNotificationScopedHandler(DisposableDependency dependency, TaskCompletionSource<TestNotification> tcs)
            {
                _dependency = dependency;
                _tcs = tcs;
            }

            public Task HandleAsync(TestNotification message, ILambdaContext context) => _tcs.Task;
        }
    }
}
