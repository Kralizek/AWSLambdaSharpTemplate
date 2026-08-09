using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;

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
    public async Task FunctionHandlerAsync_invokes_handler_for_each_record_and_passes_typed_context()
    {
        var sut = new SequentialRecordFunction();
        var lambdaContext = TestLambdaContexts.Create();
        lambdaContext.AwsRequestId = "request-id";

        var response = await sut.FunctionHandlerAsync(new[] { "a", "b", "c" }, lambdaContext);

        Assert.That(response, Is.EqualTo(3));
        Assert.That(CollectingHandler.Processed, Is.EqualTo(new[] { "a", "b", "c" }));
        Assert.That(CollectingHandler.Contexts, Has.Count.EqualTo(3));
        Assert.That(CollectingHandler.Contexts, Has.All.Property(nameof(RecordContext.AwsRequestId)).EqualTo("request-id"));
        Assert.That(CollectingHandler.Contexts, Has.All.Property(nameof(TestRecordContext.Source)).EqualTo("test"));
    }

    [Test]
    public async Task FunctionHandlerAsync_preserves_record_identity_for_response_generation()
    {
        var sut = new IdentityRecordFunction();

        var response = await sut.FunctionHandlerAsync(new[] { "a", "b" }, TestLambdaContexts.Create());

        Assert.That(response, Is.EqualTo(new[] { "a:A", "b:B" }));
    }

    [Test]
    public async Task FunctionHandlerAsync_creates_new_scope_per_record()
    {
        var sut = new ScopedRecordFunction();

        await sut.FunctionHandlerAsync(new[] { "x", "y" }, TestLambdaContexts.Create());

        Assert.That(ScopedTrackingHandler.CapturedServices, Has.Count.EqualTo(2));
        Assert.That(ScopedTrackingHandler.CapturedServices, Is.Unique);
    }

    [Test]
    public void FunctionHandlerAsync_propagates_handler_exception_by_default()
    {
        var sut = new FailingRecordFunction();

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.FunctionHandlerAsync(new[] { "bad-record" }, TestLambdaContexts.Create()));
    }

    [Test]
    public void FunctionHandlerAsync_rejects_null_handler_result()
    {
        var sut = new NullResultRecordFunction();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.FunctionHandlerAsync(new[] { "bad-record" }, TestLambdaContexts.Create()));

        Assert.That(exception!.Message, Does.Contain(nameof(NullReturningHandler)));
    }

    [Test]
    public async Task FunctionHandlerAsync_allows_specialization_to_translate_handler_exception()
    {
        var sut = new TranslatingFailureRecordFunction();

        var response = await sut.FunctionHandlerAsync(new[] { "good", "bad", "also-good" }, TestLambdaContexts.Create());

        Assert.That(response, Is.EqualTo(new[] { "good:True", "bad:False", "also-good:True" }));
        Assert.That(sut.TranslatedException, Is.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void FunctionHandlerAsync_propagates_cancellation_without_translating_it()
    {
        var sut = new CancellationRecordFunction();
        var lambdaContext = TestLambdaContexts.Create();
        lambdaContext.RemainingTime = TimeSpan.Zero;

        Assert.ThrowsAsync<OperationCanceledException>(() =>
            sut.FunctionHandlerAsync(new[] { "timed-out-record" }, lambdaContext));
        Assert.That(sut.ExceptionWasTranslated, Is.False);
    }

    [Test]
    public async Task FunctionHandlerAsync_can_use_bounded_parallel_record_processing()
    {
        var sut = new SequentialRecordFunction { MaximumDegreeOfParallelism = 2 };

        var response = await sut.FunctionHandlerAsync(
            new[] { "p1", "p2", "p3" },
            TestLambdaContexts.Create());

        Assert.That(response, Is.EqualTo(3));
        Assert.That(CollectingHandler.Processed, Is.EquivalentTo(new[] { "p1", "p2", "p3" }));
    }

    [Test]
    public async Task Bounded_parallel_processing_creates_new_scope_per_record()
    {
        var sut = new ScopedRecordFunction { MaximumDegreeOfParallelism = 2 };

        await sut.FunctionHandlerAsync(
            new[] { "p1", "p2", "p3" },
            TestLambdaContexts.Create());

        Assert.That(ScopedTrackingHandler.CapturedServices, Has.Count.EqualTo(3));
        Assert.That(ScopedTrackingHandler.CapturedServices, Is.Unique);
    }

    [Test]
    public void Bounded_parallel_processing_throws_when_parallelism_less_than_2()
    {
        var sut = new SequentialRecordFunction { MaximumDegreeOfParallelism = 1 };

        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            sut.FunctionHandlerAsync(new[] { "record" }, TestLambdaContexts.Create()));
    }

    public class SequentialRecordFunction : RecordFunction<string[], string, TestRecordResult, int, TestRecordContext, CollectingHandler>
    {
        public int? MaximumDegreeOfParallelism { get; init; }

        protected override TestRecordContext CreateRecordContext(string[] envelope, ILambdaContext lambdaContext) =>
            new(lambdaContext, "test");

        protected override IEnumerable<string> GetRecords(string[] envelope) => envelope;

        protected override int CreateResponse(IReadOnlyCollection<RecordProcessingResult> results) => results.Count;

        protected override Task<IReadOnlyCollection<RecordProcessingResult>> ProcessRecordsAsync(
            string[] envelope,
            TestRecordContext context,
            IServiceProvider invocationServices,
            CancellationToken cancellationToken) =>
            MaximumDegreeOfParallelism is { } degree
                ? ProcessRecordsParallelAsync(envelope, context, invocationServices, degree, cancellationToken)
                : base.ProcessRecordsAsync(envelope, context, invocationServices, cancellationToken);
    }

    public class IdentityRecordFunction : RecordFunction<string[], string, TestRecordResult, string[], TestRecordContext, EchoRecordHandler>
    {
        protected override TestRecordContext CreateRecordContext(string[] envelope, ILambdaContext lambdaContext) =>
            new(lambdaContext, "test");

        protected override IEnumerable<string> GetRecords(string[] envelope) => envelope;

        protected override string[] CreateResponse(IReadOnlyCollection<RecordProcessingResult> results) =>
            results.Select(result => $"{result.Record}:{result.Result.Value}").ToArray();
    }

    public class FailingRecordFunction : RecordFunction<string[], string, TestRecordResult, int, TestRecordContext, ThrowingHandler>
    {
        protected override TestRecordContext CreateRecordContext(string[] envelope, ILambdaContext lambdaContext) =>
            new(lambdaContext, "test");

        protected override IEnumerable<string> GetRecords(string[] envelope) => envelope;

        protected override int CreateResponse(IReadOnlyCollection<RecordProcessingResult> results) => results.Count;
    }

    public class NullResultRecordFunction : RecordFunction<string[], string, TestRecordResult, int, TestRecordContext, NullReturningHandler>
    {
        protected override TestRecordContext CreateRecordContext(string[] envelope, ILambdaContext lambdaContext) =>
            new(lambdaContext, "test");

        protected override IEnumerable<string> GetRecords(string[] envelope) => envelope;

        protected override int CreateResponse(IReadOnlyCollection<RecordProcessingResult> results) => results.Count;
    }

    public class TranslatingFailureRecordFunction : RecordFunction<string[], string, TestRecordResult, string[], TestRecordContext, ConditionalThrowingHandler>
    {
        public Exception? TranslatedException { get; private set; }

        protected override TestRecordContext CreateRecordContext(string[] envelope, ILambdaContext lambdaContext) =>
            new(lambdaContext, "test");

        protected override IEnumerable<string> GetRecords(string[] envelope) => envelope;

        protected override string[] CreateResponse(IReadOnlyCollection<RecordProcessingResult> results) =>
            results.Select(result => $"{result.Record}:{result.Result.Value}").ToArray();

        protected override ValueTask<TestRecordResult> HandleRecordExceptionAsync(
            string record,
            Exception exception,
            TestRecordContext context,
            CancellationToken cancellationToken)
        {
            TranslatedException = exception;
            return ValueTask.FromResult(new TestRecordResult(false));
        }
    }

    public class CancellationRecordFunction : RecordFunction<string[], string, TestRecordResult, int, TestRecordContext, CollectingHandler>
    {
        public bool ExceptionWasTranslated { get; private set; }

        protected override TestRecordContext CreateRecordContext(string[] envelope, ILambdaContext lambdaContext) =>
            new(lambdaContext, "test");

        protected override IEnumerable<string> GetRecords(string[] envelope) => envelope;

        protected override int CreateResponse(IReadOnlyCollection<RecordProcessingResult> results) => results.Count;

        protected override ValueTask<TestRecordResult> HandleRecordExceptionAsync(
            string record,
            Exception exception,
            TestRecordContext context,
            CancellationToken cancellationToken)
        {
            ExceptionWasTranslated = true;
            return ValueTask.FromResult(new TestRecordResult(false));
        }
    }

    public class ScopedRecordFunction : RecordFunction<string[], string, TestRecordResult, int, TestRecordContext, ScopedTrackingHandler>
    {
        public int? MaximumDegreeOfParallelism { get; init; }

        protected override TestRecordContext CreateRecordContext(string[] envelope, ILambdaContext lambdaContext) =>
            new(lambdaContext, "test");

        protected override IEnumerable<string> GetRecords(string[] envelope) => envelope;

        protected override int CreateResponse(IReadOnlyCollection<RecordProcessingResult> results) => results.Count;

        protected override Task<IReadOnlyCollection<RecordProcessingResult>> ProcessRecordsAsync(
            string[] envelope,
            TestRecordContext context,
            IServiceProvider invocationServices,
            CancellationToken cancellationToken) =>
            MaximumDegreeOfParallelism is { } degree
                ? ProcessRecordsParallelAsync(envelope, context, invocationServices, degree, cancellationToken)
                : base.ProcessRecordsAsync(envelope, context, invocationServices, cancellationToken);

        protected override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ScopedService>();
        }
    }

    public sealed class TestRecordContext : RecordContext
    {
        public TestRecordContext(ILambdaContext lambdaContext, string source)
            : base(FunctionContextFactory.CreateMetadata(lambdaContext), FunctionContextFactory.CreateProperties(lambdaContext))
        {
            Source = source;
        }

        public string Source { get; }
    }

    public sealed class TestRecordResult : LambdaRecordResult
    {
        public TestRecordResult(object? value)
        {
            Value = value;
        }

        public override object? Value { get; }
    }

    public class CollectingHandler : IRecordHandler<string, TestRecordResult, TestRecordContext>
    {
        public static ConcurrentQueue<string> Processed { get; } = new();
        public static ConcurrentBag<TestRecordContext> Contexts { get; } = new();

        public ValueTask<TestRecordResult> HandleAsync(string record, TestRecordContext context, CancellationToken cancellationToken)
        {
            Processed.Enqueue(record);
            Contexts.Add(context);
            return ValueTask.FromResult(new TestRecordResult(true));
        }
    }

    public class EchoRecordHandler : IRecordHandler<string, TestRecordResult, TestRecordContext>
    {
        public ValueTask<TestRecordResult> HandleAsync(string record, TestRecordContext context, CancellationToken cancellationToken) =>
            ValueTask.FromResult(new TestRecordResult(record.ToUpperInvariant()));
    }

    public class ThrowingHandler : IRecordHandler<string, TestRecordResult, TestRecordContext>
    {
        public ValueTask<TestRecordResult> HandleAsync(string record, TestRecordContext context, CancellationToken cancellationToken) =>
            ValueTask.FromException<TestRecordResult>(new InvalidOperationException("handler failed"));
    }

    public class NullReturningHandler : IRecordHandler<string, TestRecordResult, TestRecordContext>
    {
        public ValueTask<TestRecordResult> HandleAsync(string record, TestRecordContext context, CancellationToken cancellationToken) =>
            ValueTask.FromResult<TestRecordResult>(null!);
    }

    public class ConditionalThrowingHandler : IRecordHandler<string, TestRecordResult, TestRecordContext>
    {
        public ValueTask<TestRecordResult> HandleAsync(string record, TestRecordContext context, CancellationToken cancellationToken) =>
            record == "bad"
                ? ValueTask.FromException<TestRecordResult>(new InvalidOperationException("handler failed"))
                : ValueTask.FromResult(new TestRecordResult(true));
    }

    public class ScopedTrackingHandler : IRecordHandler<string, TestRecordResult, TestRecordContext>
    {
        public static ConcurrentBag<ScopedService> CapturedServices { get; } = new();

        private readonly ScopedService _service;

        public ScopedTrackingHandler(ScopedService service)
        {
            _service = service;
        }

        public ValueTask<TestRecordResult> HandleAsync(string record, TestRecordContext context, CancellationToken cancellationToken)
        {
            CapturedServices.Add(_service);
            return ValueTask.FromResult(new TestRecordResult(true));
        }
    }

    public class ScopedService { }
}