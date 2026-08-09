using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.SNSEvents;

using Kralizek.Lambda;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

namespace Tests.Lambda.Sns;

[TestFixture]
public class SnsFunctionTests
{
    [SetUp]
    public void SetUp()
    {
        TestHandler.Reset();
        ScopedHandler.Reset();
        ConcurrencyHandler.Reset();
    }

    [Test]
    public async Task Function_decodes_notifications_and_forwards_record_context()
    {
        var function = new TestFunction();
        var lambdaContext = TestLambdaContexts.Create();
        var @event = CreateEvent(
            ("first", "{\"value\":\"one\"}"),
            ("second", "{\"value\":\"two\"}"));
        var second = @event.Records[1];
        second.EventSource = "aws:sns";
        second.EventSubscriptionArn = "arn:aws:sns:eu-north-1:123456789012:orders:subscription";
        second.EventVersion = "1.0";
        second.Sns.TopicArn = "arn:aws:sns:eu-north-1:123456789012:orders";
        second.Sns.Subject = "Order created";
        second.Sns.Type = "Notification";
        second.Sns.Signature = "signature";
        second.Sns.SignatureVersion = "1";
        second.Sns.SigningCertUrl = "https://example.com/cert.pem";
        second.Sns.UnsubscribeUrl = "https://example.com/unsubscribe";
        second.Sns.MessageAttributes = new Dictionary<string, SNSEvent.MessageAttribute>
        {
            ["tenant"] = new() { Type = "String", Value = "example" }
        };

        var response = await function.FunctionHandlerAsync(@event, lambdaContext);

        Assert.Multiple(() =>
        {
            Assert.That(response, Is.Null);
            Assert.That(TestHandler.Notifications.Select(notification => notification.Value), Is.EqualTo(new[] { "one", "two" }));
            Assert.That(TestHandler.LastContext?.MessageId, Is.EqualTo("second"));
            Assert.That(TestHandler.LastContext?.EventSource, Is.EqualTo("aws:sns"));
            Assert.That(TestHandler.LastContext?.EventSubscriptionArn, Does.Contain("subscription"));
            Assert.That(TestHandler.LastContext?.EventVersion, Is.EqualTo("1.0"));
            Assert.That(TestHandler.LastContext?.TopicArn, Does.EndWith(":orders"));
            Assert.That(TestHandler.LastContext?.Subject, Is.EqualTo("Order created"));
            Assert.That(TestHandler.LastContext?.Type, Is.EqualTo("Notification"));
            Assert.That(TestHandler.LastContext?.MessageAttributes["tenant"].Value, Is.EqualTo("example"));
            Assert.That(TestHandler.LastContext?.GetSnsRecord(), Is.SameAs(second));
            Assert.That(TestHandler.LastContext?.AwsRequestId, Is.EqualTo(lambdaContext.AwsRequestId));
            Assert.That(TestHandler.LastContext?.GetLambdaContext(), Is.SameAs(lambdaContext));
            Assert.That(SnsRecordResult.Completed.Value, Is.TypeOf<SnsRecordResult.CompletionCase>());
        });
    }

    [Test]
    public async Task Consumer_can_replace_default_decoder()
    {
        var function = new PlainTextFunction();
        var @event = CreateEvent(("text", "plain text"));

        await function.FunctionHandlerAsync(@event, TestLambdaContexts.Create());

        Assert.That(TestHandler.Notifications.Single().Value, Is.EqualTo("plain text"));
    }

    [Test]
    public void Record_failure_fails_the_whole_invocation()
    {
        var function = new TestFunction();
        var @event = CreateEvent(
            ("ok", "{\"value\":\"ok\"}"),
            ("failed", "{\"value\":\"fail\"}"));

        Assert.That(
            async () => await function.FunctionHandlerAsync(@event, TestLambdaContexts.Create()),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public async Task Null_record_list_is_treated_as_an_empty_batch()
    {
        var function = new TestFunction();
        var @event = new SNSEvent { Records = null! };

        var response = await function.FunctionHandlerAsync(@event, TestLambdaContexts.Create());

        Assert.Multiple(() =>
        {
            Assert.That(response, Is.Null);
            Assert.That(TestHandler.Notifications, Is.Empty);
        });
    }

    [Test]
    public async Task Handler_is_scoped_and_disposed_for_each_record()
    {
        var function = new ScopedFunction();
        var @event = CreateEvent(
            ("first", "{\"value\":\"one\"}"),
            ("second", "{\"value\":\"two\"}"));

        await function.FunctionHandlerAsync(@event, TestLambdaContexts.Create());

        Assert.Multiple(() =>
        {
            Assert.That(ScopedHandler.InstanceIds.Distinct().Count(), Is.EqualTo(2));
            Assert.That(ScopedHandler.DisposedCount, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task Parallel_function_honors_the_configured_degree_of_parallelism()
    {
        var function = new TestParallelFunction();
        var @event = CreateEvent(
            ("first", "{\"value\":\"one\"}"),
            ("second", "{\"value\":\"two\"}"),
            ("third", "{\"value\":\"three\"}"),
            ("fourth", "{\"value\":\"four\"}"));

        await function.FunctionHandlerAsync(@event, TestLambdaContexts.Create());

        Assert.Multiple(() =>
        {
            Assert.That(ConcurrencyHandler.ProcessedCount, Is.EqualTo(4));
            Assert.That(ConcurrencyHandler.MaxConcurrency, Is.LessThanOrEqualTo(2));
        });
    }

    private static SNSEvent CreateEvent(params (string Id, string Message)[] records) =>
        new()
        {
            Records = records
                .Select(record => new SNSEvent.SNSRecord
                {
                    Sns = new SNSEvent.SNSMessage
                    {
                        MessageId = record.Id,
                        Message = record.Message,
                        Timestamp = DateTime.UtcNow
                    }
                })
                .ToList()
        };

    private sealed class TestFunction : SnsFunction<TestNotification, TestHandler>;

    private sealed class ScopedFunction : SnsFunction<TestNotification, ScopedHandler>;

    private sealed class TestParallelFunction : ParallelSnsFunction<TestNotification, ConcurrencyHandler>
    {
        protected override int MaxDegreeOfParallelism => 2;
    }

    private sealed class PlainTextFunction : SnsFunction<TestNotification, TestHandler>
    {
        protected override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IStringPayloadDecoder<TestNotification>, PlainTextDecoder>();
        }
    }

    private sealed class PlainTextDecoder : IStringPayloadDecoder<TestNotification>
    {
        public ValueTask<TestNotification> DecodeAsync(string payload, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(new TestNotification(payload));
        }
    }

    private sealed class TestHandler : ISnsNotificationHandler<TestNotification>
    {
        private static readonly ConcurrentQueue<TestNotification> ReceivedNotifications = new();

        public static IReadOnlyCollection<TestNotification> Notifications => ReceivedNotifications.ToArray();

        public static SnsNotificationContext? LastContext { get; private set; }

        public static void Reset()
        {
            ReceivedNotifications.Clear();
            LastContext = null;
        }

        public ValueTask<SnsRecordResult> HandleAsync(
            TestNotification notification,
            SnsNotificationContext context,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastContext = context;

            if (notification.Value == "fail")
            {
                throw new InvalidOperationException("Expected test failure.");
            }

            ReceivedNotifications.Enqueue(notification);
            return ValueTask.FromResult(SnsRecordResult.Completed);
        }
    }

    private sealed class ScopedHandler : ISnsNotificationHandler<TestNotification>, IDisposable
    {
        private readonly Guid _instanceId = Guid.NewGuid();
        private static readonly ConcurrentQueue<Guid> Instances = new();
        private static int _disposedCount;

        public static IReadOnlyCollection<Guid> InstanceIds => Instances.ToArray();

        public static int DisposedCount => _disposedCount;

        public static void Reset()
        {
            Instances.Clear();
            _disposedCount = 0;
        }

        public ValueTask<SnsRecordResult> HandleAsync(
            TestNotification notification,
            SnsNotificationContext context,
            CancellationToken cancellationToken)
        {
            Instances.Enqueue(_instanceId);
            return ValueTask.FromResult(SnsRecordResult.Completed);
        }

        public void Dispose() => Interlocked.Increment(ref _disposedCount);
    }

    private sealed class ConcurrencyHandler : ISnsNotificationHandler<TestNotification>
    {
        private static int _currentConcurrency;
        private static int _maxConcurrency;
        private static int _processedCount;

        public static int MaxConcurrency => _maxConcurrency;

        public static int ProcessedCount => _processedCount;

        public static void Reset()
        {
            _currentConcurrency = 0;
            _maxConcurrency = 0;
            _processedCount = 0;
        }

        public async ValueTask<SnsRecordResult> HandleAsync(
            TestNotification notification,
            SnsNotificationContext context,
            CancellationToken cancellationToken)
        {
            var current = Interlocked.Increment(ref _currentConcurrency);
            UpdateMaximum(current);

            try
            {
                await Task.Delay(25, cancellationToken);
                Interlocked.Increment(ref _processedCount);
                return SnsRecordResult.Completed;
            }
            finally
            {
                Interlocked.Decrement(ref _currentConcurrency);
            }
        }

        private static void UpdateMaximum(int current)
        {
            int observed;

            do
            {
                observed = _maxConcurrency;
                if (current <= observed)
                {
                    return;
                }
            }
            while (Interlocked.CompareExchange(ref _maxConcurrency, current, observed) != observed);
        }
    }

    private sealed record TestNotification(string Value);
}