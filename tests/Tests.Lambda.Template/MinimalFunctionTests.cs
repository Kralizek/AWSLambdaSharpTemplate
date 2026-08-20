using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;

using Kralizek.Lambda;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NUnit.Framework;

namespace Tests.Lambda;

[TestFixture]
public class MinimalFunctionTests
{
    [SetUp]
    public void SetUp()
    {
        TrackingEventHandler.Reset();
        TrackingRequestHandler.Reset();
        ScopedTrackingHandler.ScopeIds.Clear();
        AsyncDisposableDependency.WasDisposed = false;
    }

    [Test]
    public void MinimalEventFunction_preserves_configuration_service_and_logging_hooks()
    {
        var sut = new ConfiguredMinimalEventFunction();

        Assert.Multiple(() =>
        {
            Assert.That(sut.IsConfigureConfigurationInvoked, Is.True);
            Assert.That(sut.IsConfigureServicesInvoked, Is.True);
            Assert.That(sut.IsConfigureLoggingInvoked, Is.True);
        });
    }

    [Test]
    public async Task MinimalEventFunction_invokes_existing_handler_contract_with_standard_context()
    {
        var sut = new TrackingMinimalEventFunction();
        var lambdaContext = TestLambdaContexts.Create();
        lambdaContext.AwsRequestId = "minimal-event";

        await sut.FunctionHandlerAsync("expected", lambdaContext);

        Assert.Multiple(() =>
        {
            Assert.That(TrackingEventHandler.Input, Is.EqualTo("expected"));
            Assert.That(TrackingEventHandler.Context?.AwsRequestId, Is.EqualTo("minimal-event"));
            Assert.That(TrackingEventHandler.Context?.GetLambdaContext(), Is.SameAs(lambdaContext));
        });
    }

    [Test]
    public async Task MinimalRequestFunction_invokes_existing_handler_contract_with_standard_context()
    {
        var sut = new TrackingMinimalRequestFunction();
        var lambdaContext = TestLambdaContexts.Create();
        lambdaContext.AwsRequestId = "minimal-request";

        var result = await sut.FunctionHandlerAsync("hello", lambdaContext);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo("HELLO"));
            Assert.That(TrackingRequestHandler.Context?.AwsRequestId, Is.EqualTo("minimal-request"));
            Assert.That(TrackingRequestHandler.Context?.GetLambdaContext(), Is.SameAs(lambdaContext));
        });
    }

    [Test]
    public void MinimalEventFunction_cancels_before_handler_when_no_execution_time_remains()
    {
        var sut = new TrackingMinimalEventFunction();
        var lambdaContext = new TestLambdaContext { RemainingTime = TimeSpan.Zero };

        Assert.ThrowsAsync<OperationCanceledException>(() => sut.FunctionHandlerAsync("hello", lambdaContext));
        Assert.That(TrackingEventHandler.Input, Is.Null);
    }

    [Test]
    public async Task MinimalEventFunction_creates_an_independent_scope_per_invocation()
    {
        var sut = new ScopedMinimalEventFunction();
        var context = TestLambdaContexts.Create();

        await sut.FunctionHandlerAsync("first", context);
        await sut.FunctionHandlerAsync("second", context);

        Assert.That(ScopedTrackingHandler.ScopeIds, Has.Count.EqualTo(2));
        Assert.That(ScopedTrackingHandler.ScopeIds[0], Is.Not.EqualTo(ScopedTrackingHandler.ScopeIds[1]));
    }

    [Test]
    public async Task MinimalEventFunction_async_disposes_scoped_dependencies_after_handler_completion()
    {
        var sut = new AsyncDisposableMinimalEventFunction();

        await sut.FunctionHandlerAsync("trigger", TestLambdaContexts.Create());

        Assert.That(AsyncDisposableDependency.WasDisposed, Is.True);
    }

    [Test]
    public async Task MinimalEventFunction_passes_specialized_context_without_changing_handler_contract()
    {
        SpecializedMinimalEventHandler.ReceivedContext = null;
        var sut = new SpecializedMinimalEventFunction();
        var lambdaContext = TestLambdaContexts.Create();

        await sut.FunctionHandlerAsync("expected", lambdaContext);

        Assert.That(SpecializedMinimalEventHandler.ReceivedContext?.Input, Is.EqualTo("expected"));
        Assert.That(SpecializedMinimalEventHandler.ReceivedContext?.GetLambdaContext(), Is.SameAs(lambdaContext));
    }

    public sealed class ConfiguredMinimalEventFunction : MinimalEventFunction<string, TrackingEventHandler>
    {
        protected override void ConfigureConfiguration(IConfigurationBuilder configuration) => IsConfigureConfigurationInvoked = true;
        protected override void ConfigureServices(IServiceCollection services, IConfiguration configuration) => IsConfigureServicesInvoked = true;
        protected override void ConfigureLogging(ILoggingBuilder logging) => IsConfigureLoggingInvoked = true;

        public bool IsConfigureConfigurationInvoked { get; private set; }
        public bool IsConfigureServicesInvoked { get; private set; }
        public bool IsConfigureLoggingInvoked { get; private set; }
    }

    public sealed class TrackingMinimalEventFunction : MinimalEventFunction<string, TrackingEventHandler> { }

    public sealed class TrackingMinimalRequestFunction : MinimalRequestFunction<string, string, TrackingRequestHandler> { }

    public sealed class ScopedMinimalEventFunction : MinimalEventFunction<string, ScopedTrackingHandler>
    {
        protected override void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
            services.AddScoped<ScopedDependency>();
    }

    public sealed class AsyncDisposableMinimalEventFunction : MinimalEventFunction<string, AsyncDisposableHandler>
    {
        protected override void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
            services.AddScoped<AsyncDisposableDependency>();
    }

    public sealed class SpecializedMinimalEventFunction : MinimalEventFunction<string, SpecializedEventContext, SpecializedMinimalEventHandler>
    {
        protected override SpecializedEventContext CreateContext(string input, ILambdaContext context) => new(context, input);
    }

    public sealed class SpecializedEventContext : EventContext
    {
        public SpecializedEventContext(ILambdaContext context, string input)
            : base(FunctionContextFactory.CreateMetadata(context), FunctionContextFactory.CreateProperties(context))
        {
            Input = input;
        }

        public string Input { get; }
    }

    public sealed class TrackingEventHandler : IEventHandler<string>
    {
        public static string? Input { get; private set; }
        public static EventContext? Context { get; private set; }

        public static void Reset()
        {
            Input = null;
            Context = null;
        }

        public ValueTask HandleAsync(string input, EventContext context, CancellationToken cancellationToken)
        {
            Input = input;
            Context = context;
            return ValueTask.CompletedTask;
        }
    }

    public sealed class TrackingRequestHandler : IRequestHandler<string, string>
    {
        public static RequestContext? Context { get; private set; }

        public static void Reset() => Context = null;

        public ValueTask<string> HandleAsync(string input, RequestContext context, CancellationToken cancellationToken)
        {
            Context = context;
            return ValueTask.FromResult(input.ToUpperInvariant());
        }
    }

    public sealed class ScopedDependency
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    public sealed class ScopedTrackingHandler : IEventHandler<string>
    {
        private readonly ScopedDependency _dependency;

        public ScopedTrackingHandler(ScopedDependency dependency) => _dependency = dependency;

        public static List<Guid> ScopeIds { get; } = [];

        public ValueTask HandleAsync(string input, EventContext context, CancellationToken cancellationToken)
        {
            ScopeIds.Add(_dependency.Id);
            return ValueTask.CompletedTask;
        }
    }

    public sealed class AsyncDisposableDependency : IAsyncDisposable
    {
        public static bool WasDisposed { get; set; }

        public ValueTask DisposeAsync()
        {
            WasDisposed = true;
            return ValueTask.CompletedTask;
        }
    }

    public sealed class AsyncDisposableHandler : IEventHandler<string>
    {
        private readonly AsyncDisposableDependency _dependency;

        public AsyncDisposableHandler(AsyncDisposableDependency dependency) => _dependency = dependency;

        public ValueTask HandleAsync(string input, EventContext context, CancellationToken cancellationToken)
        {
            _ = _dependency;
            return ValueTask.CompletedTask;
        }
    }

    public sealed class SpecializedMinimalEventHandler : IEventHandler<string, SpecializedEventContext>
    {
        public static SpecializedEventContext? ReceivedContext { get; set; }

        public ValueTask HandleAsync(string input, SpecializedEventContext context, CancellationToken cancellationToken)
        {
            ReceivedContext = context;
            return ValueTask.CompletedTask;
        }
    }
}
