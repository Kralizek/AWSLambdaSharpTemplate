using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.TestUtilities;

using Kralizek.Lambda;

using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

namespace Tests.Lambda;

[TestFixture]
public class RecordFunctionTests
{
    [SetUp]
    public void SetUp()
    {
        CollectingHandler.Processed.Clear();
        ScopedTrackingHandler.CapturedServices.Clear();
    }

    [Test]
    public async Task ProcessRecordsAsync_invokes_handler_for_each_record()
    {
        var sut = new SequentialRecordFunction();

        await sut.InvokeAsync(new[] { "a", "b", "c" }, new TestLambdaContext());

        Assert.That(CollectingHandler.Processed, Is.EqualTo(new[] { "a", "b", "c" }));
    }

    [Test]
    public async Task ProcessRecordsAsync_creates_new_scope_per_record()
    {
        var sut = new ScopedRecordFunction();

        await sut.InvokeAsync(new[] { "x", "y" }, new TestLambdaContext());

        Assert.That(ScopedTrackingHandler.CapturedServices.Count, Is.EqualTo(2));
        Assert.That(ScopedTrackingHandler.CapturedServices[0], Is.Not.SameAs(ScopedTrackingHandler.CapturedServices[1]));
    }

    [Test]
    public void ProcessRecordsAsync_propagates_handler_exception()
    {
        var sut = new FailingRecordFunction();

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.InvokeAsync(new[] { "bad-record" }, new TestLambdaContext()));
    }

    [Test]
    public async Task ProcessRecordsParallelAsync_processes_all_records()
    {
        var sut = new SequentialRecordFunction();

        await sut.InvokeParallelAsync(new[] { "p1", "p2", "p3" }, maxDegreeOfParallelism: 2, new TestLambdaContext());

        Assert.That(CollectingHandler.Processed, Is.EquivalentTo(new[] { "p1", "p2", "p3" }));
    }

    [Test]
    public void ProcessRecordsParallelAsync_throws_when_parallelism_less_than_2()
    {
        var sut = new SequentialRecordFunction();

        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            sut.InvokeParallelAsync(new[] { "r" }, maxDegreeOfParallelism: 1, new TestLambdaContext()));
    }

    public class SequentialRecordFunction : RecordFunction<string[], string, CollectingHandler>
    {
        protected override IEnumerable<string> GetRecords(string[] @event) => @event;

        public Task InvokeAsync(string[] records, Amazon.Lambda.Core.ILambdaContext context)
        {
            using var cts = CreateCancellationTokenSource(context);
            return ProcessRecordsAsync(records, cts.Token);
        }

        public Task InvokeParallelAsync(string[] records, int maxDegreeOfParallelism, Amazon.Lambda.Core.ILambdaContext context)
        {
            using var cts = CreateCancellationTokenSource(context);
            return ProcessRecordsParallelAsync(records, maxDegreeOfParallelism, cts.Token);
        }
    }

    public class FailingRecordFunction : RecordFunction<string[], string, ThrowingHandler>
    {
        protected override IEnumerable<string> GetRecords(string[] @event) => @event;

        public Task InvokeAsync(string[] records, Amazon.Lambda.Core.ILambdaContext context)
        {
            using var cts = CreateCancellationTokenSource(context);
            return ProcessRecordsAsync(records, cts.Token);
        }
    }

    public class ScopedRecordFunction : RecordFunction<string[], string, ScopedTrackingHandler>
    {
        protected override IEnumerable<string> GetRecords(string[] @event) => @event;

        protected override void ConfigureServices(IServiceCollection services, IExecutionEnvironment executionEnvironment)
        {
            services.AddScoped<ScopedService>();
        }

        public Task InvokeAsync(string[] records, Amazon.Lambda.Core.ILambdaContext context)
        {
            using var cts = CreateCancellationTokenSource(context);
            return ProcessRecordsAsync(records, cts.Token);
        }
    }

    public class CollectingHandler : IRecordHandler<string>
    {
        public static ConcurrentQueue<string> Processed { get; } = new();

        public ValueTask HandleAsync(string record, CancellationToken cancellationToken)
        {
            Processed.Enqueue(record);
            return ValueTask.CompletedTask;
        }
    }

    public class ThrowingHandler : IRecordHandler<string>
    {
        public ValueTask HandleAsync(string record, CancellationToken cancellationToken) =>
            ValueTask.FromException(new InvalidOperationException("handler failed"));
    }

    public class ScopedTrackingHandler : IRecordHandler<string>
    {
        public static List<ScopedService> CapturedServices { get; } = new();

        private readonly ScopedService _service;

        public ScopedTrackingHandler(ScopedService service)
        {
            _service = service;
        }

        public ValueTask HandleAsync(string record, CancellationToken cancellationToken)
        {
            CapturedServices.Add(_service);
            return ValueTask.CompletedTask;
        }
    }

    public class ScopedService { }
}