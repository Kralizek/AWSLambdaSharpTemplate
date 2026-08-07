using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;

using Kralizek.Lambda;

using Microsoft.Extensions.Configuration;
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
        CollectingHandler.Contexts.Clear();
        ScopedTrackingHandler.CapturedServices.Clear();
    }

    [Test]
    public async Task ProcessRecordsAsync_invokes_handler_for_each_record_and_passes_context()
    {
        var sut = new SequentialRecordFunction();
        var lambdaContext = new TestLambdaContext { AwsRequestId = "request-id" };

        var response = await sut.InvokeAsync(new[] { "a", "b", "c" }, lambdaContext);

        Assert.That(response, Is.EqualTo(3));
        Assert.That(CollectingHandler.Processed, Is.EqualTo(new[] { "a", "b", "c" }));
        Assert.That(CollectingHandler.Contexts, Has.Count.EqualTo(3));
        Assert.That(CollectingHandler.Contexts, Has.All.Property(nameof(RecordContext.AwsRequestId)).EqualTo("request-id"));
    }

    [Test]
    public async Task ProcessRecordsAsync_creates_new_scope_per_record()
    {
        var sut = new ScopedRecordFunction();

        await sut.InvokeAsync(new[] { "x", "y" }, new TestLambdaContext());

        Assert.That(ScopedTrackingHandler.CapturedServices, Has.Count.EqualTo(2));
        Assert.That(ScopedTrackingHandler.CapturedServices, Is.Unique);
    }

    [Test]
    public void ProcessRecordsAsync_propagates_handler_exception()
    {
        var sut = new FailingRecordFunction();

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.InvokeAsync(new[] { "bad-record" }, new TestLambdaContext()));
    }

    [Test]
    public void ProcessRecordsAsync_propagates_cancellation()
    {
        var sut = new CancellationRecordFunction();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(() =>
            sut.InvokeAsync(new[] { "timed-out-record" }, cancellation.Token));
    }

    [Test]
    public async Task ProcessRecordsParallelAsync_processes_all_records()
    {
        var sut = new SequentialRecordFunction();

        var response = await sut.InvokeParallelAsync(
            new[] { "p1", "p2", "p3" },
            maxDegreeOfParallelism: 2,
            new TestLambdaContext());

        Assert.That(response, Is.EqualTo(3));
        Assert.That(CollectingHandler.Processed, Is.EquivalentTo(new[] { "p1", "p2", "p3" }));
    }

    [Test]
    public async Task ProcessRecordsParallelAsync_creates_new_scope_per_record()
    {
        var sut = new ScopedRecordFunction();

        await sut.InvokeParallelAsync(
            new[] { "p1", "p2", "p3" },
            maxDegreeOfParallelism: 2,
            new TestLambdaContext());

        Assert.That(ScopedTrackingHandler.CapturedServices, Has.Count.EqualTo(3));
        Assert.That(ScopedTrackingHandler.CapturedServices, Is.Unique);
    }

    [Test]
    public void ProcessRecordsParallelAsync_throws_when_parallelism_less_than_2()
    {
        var sut = new SequentialRecordFunction();

        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            sut.InvokeParallelAsync(
                new[] { "record" },
                maxDegreeOfParallelism: 1,
                new TestLambdaContext()));
    }

    public class SequentialRecordFunction : RecordFunction<string[], string, bool, int, CollectingHandler>
    {
        protected override IEnumerable<string> GetRecords(string[] envelope) => envelope;

        protected override int CreateResponse(IReadOnlyCollection<bool> results) => results.Count;

        public Task<int> InvokeAsync(string[] records, ILambdaContext context)
        {
            using var cts = CreateCancellationTokenSource(context);
            return ProcessRecordsAsync(records, CreateRecordContext(context), cts.Token);
        }

        public Task<int> InvokeParallelAsync(
            string[] records,
            int maxDegreeOfParallelism,
            ILambdaContext context)
        {
            using var cts = CreateCancellationTokenSource(context);
            return ProcessRecordsParallelAsync(
                records,
                CreateRecordContext(context),
                maxDegreeOfParallelism,
                cts.Token);
        }
    }

    public class FailingRecordFunction : RecordFunction<string[], string, bool, int, ThrowingHandler>
    {
        protected override IEnumerable<string> GetRecords(string[] envelope) => envelope;

        protected override int CreateResponse(IReadOnlyCollection<bool> results) => results.Count;

        public Task<int> InvokeAsync(string[] records, ILambdaContext context)
        {
            using var cts = CreateCancellationTokenSource(context);
            return ProcessRecordsAsync(records, CreateRecordContext(context), cts.Token);
        }
    }

    public class CancellationRecordFunction : RecordFunction<string[], string, bool, int, CollectingHandler>
    {
        protected override IEnumerable<string> GetRecords(string[] envelope) => envelope;

        protected override int CreateResponse(IReadOnlyCollection<bool> results) => results.Count;

        public Task<int> InvokeAsync(string[] records, CancellationToken cancellationToken) =>
            ProcessRecordsAsync(records, CreateRecordContext(new TestLambdaContext()), cancellationToken);
    }

    public class ScopedRecordFunction : RecordFunction<string[], string, bool, int, ScopedTrackingHandler>
    {
        protected override IEnumerable<string> GetRecords(string[] envelope) => envelope;

        protected override int CreateResponse(IReadOnlyCollection<bool> results) => results.Count;

        protected override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ScopedService>();
        }

        public Task<int> InvokeAsync(string[] records, ILambdaContext context)
        {
            using var cts = CreateCancellationTokenSource(context);
            return ProcessRecordsAsync(records, CreateRecordContext(context), cts.Token);
        }

        public Task<int> InvokeParallelAsync(
            string[] records,
            int maxDegreeOfParallelism,
            ILambdaContext context)
        {
            using var cts = CreateCancellationTokenSource(context);
            return ProcessRecordsParallelAsync(
                records,
                CreateRecordContext(context),
                maxDegreeOfParallelism,
                cts.Token);
        }
    }

    public class CollectingHandler : IRecordHandler<string, bool>
    {
        public static ConcurrentQueue<string> Processed { get; } = new();
        public static ConcurrentBag<RecordContext> Contexts { get; } = new();

        public ValueTask<bool> HandleAsync(string record, RecordContext context, CancellationToken cancellationToken)
        {
            Processed.Enqueue(record);
            Contexts.Add(context);
            return new ValueTask<bool>(true);
        }
    }

    public class ThrowingHandler : IRecordHandler<string, bool>
    {
        public ValueTask<bool> HandleAsync(string record, RecordContext context, CancellationToken cancellationToken) =>
            ValueTask.FromException<bool>(new InvalidOperationException("handler failed"));
    }

    public class ScopedTrackingHandler : IRecordHandler<string, bool>
    {
        public static ConcurrentBag<ScopedService> CapturedServices { get; } = new();

        private readonly ScopedService _service;

        public ScopedTrackingHandler(ScopedService service)
        {
            _service = service;
        }

        public ValueTask<bool> HandleAsync(string record, RecordContext context, CancellationToken cancellationToken)
        {
            CapturedServices.Add(_service);
            return new ValueTask<bool>(true);
        }
    }

    public class ScopedService { }
}