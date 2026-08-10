using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.S3Events;
using Amazon.Lambda.SQSEvents;

using Kralizek.Lambda;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NUnit.Framework;

namespace Tests.Lambda.S3;

[TestFixture]
public class NestedS3ProcessingTests
{
    [SetUp]
    public void SetUp() => NestedS3Handler.Reset();

    [Test]
    public async Task Nested_S3_records_get_independent_scopes_and_preserve_SQS_context()
    {
        var sqsEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                CreateSqsMessage("message-1", "one.txt", "two.txt"),
                CreateSqsMessage("message-2", "three.txt", "four.txt", "five.txt")
            }
        };

        var response = await new NestedFunction().FunctionHandlerAsync(sqsEvent, TestLambdaContexts.Create());

        Assert.Multiple(() =>
        {
            Assert.That(response.BatchItemFailures, Is.Empty);
            Assert.That(NestedS3Handler.Received, Has.Count.EqualTo(5));
            Assert.That(NestedS3Handler.Received.Select(x => x.ScopeId).Distinct().Count(), Is.EqualTo(5));
            Assert.That(NestedS3Handler.Received.Count(x => x.SqsMessageId == "message-1"), Is.EqualTo(2));
            Assert.That(NestedS3Handler.Received.Count(x => x.SqsMessageId == "message-2"), Is.EqualTo(3));
        });
    }

    [Test]
    public async Task Inner_S3_failure_marks_only_the_containing_SQS_message_as_failed()
    {
        NestedS3Handler.FailOnKey = "fail.txt";

        var sqsEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                CreateSqsMessage("message-1", "before.txt", "fail.txt", "after.txt"),
                CreateSqsMessage("message-2", "other.txt")
            }
        };

        var response = await new NestedFunction().FunctionHandlerAsync(sqsEvent, TestLambdaContexts.Create());

        Assert.Multiple(() =>
        {
            Assert.That(response.BatchItemFailures.Select(x => x.ItemIdentifier), Is.EqualTo(new[] { "message-1" }));
            Assert.That(NestedS3Handler.Received.Select(x => x.Key), Is.EqualTo(new[] { "before.txt", "fail.txt", "other.txt" }));
        });
    }

    private static SQSEvent.SQSMessage CreateSqsMessage(string messageId, params string[] keys) =>
        new()
        {
            MessageId = messageId,
            Body = JsonSerializer.Serialize(CreateS3Event(keys), new JsonSerializerOptions(JsonSerializerDefaults.Web))
        };

    private static S3Event CreateS3Event(IEnumerable<string> keys) =>
        new()
        {
            Records = keys.Select(key => new S3Event.S3EventNotificationRecord
            {
                EventName = "ObjectCreated:Put",
                EventTime = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc),
                S3 = new S3Event.S3Entity
                {
                    Bucket = new S3Event.S3BucketEntity { Name = "uploads" },
                    Object = new S3Event.S3ObjectEntity { Key = key }
                }
            }).ToList()
        };

    private sealed class NestedFunction : SqsFunction<S3Event, NestedSqsHandler>
    {
        protected override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddS3ObjectEventProcessing<NestedS3Handler>();
            services.TryAddScoped<NestedDispatcher>();
            services.TryAddScoped<ScopeMarker>();
        }
    }

    private sealed class NestedSqsHandler : ISqsMessageHandler<S3Event>
    {
        private readonly NestedDispatcher _dispatcher;

        public NestedSqsHandler(NestedDispatcher dispatcher) => _dispatcher = dispatcher;

        public async ValueTask<SqsRecordResult> HandleAsync(
            S3Event message,
            SqsMessageContext context,
            CancellationToken cancellationToken)
        {
            await _dispatcher.DispatchAsync(message, context, cancellationToken).ConfigureAwait(false);
            return SqsRecordResult.Success;
        }
    }

    private sealed class NestedDispatcher
    {
        private readonly IRecordProcessor<S3Event.S3EventNotificationRecord, S3RecordResult, RecordContext> _processor;

        public NestedDispatcher(
            IRecordProcessor<S3Event.S3EventNotificationRecord, S3RecordResult, RecordContext> processor) =>
            _processor = processor;

        public async ValueTask DispatchAsync(
            S3Event s3Event,
            RecordContext context,
            CancellationToken cancellationToken)
        {
            foreach (var record in s3Event.Records)
            {
                await _processor.ProcessAsync(record, context, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private sealed class ScopeMarker
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    private sealed class NestedS3Handler : IS3ObjectEventHandler
    {
        private static readonly List<ReceivedItem> Items = new();
        private readonly ScopeMarker _scopeMarker;

        public NestedS3Handler(ScopeMarker scopeMarker) => _scopeMarker = scopeMarker;

        public static IReadOnlyCollection<ReceivedItem> Received => Items;
        public static string? FailOnKey { get; set; }

        public static void Reset()
        {
            Items.Clear();
            FailOnKey = null;
        }

        public ValueTask HandleAsync(
            S3ObjectEvent item,
            S3RecordContext context,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Items.Add(new ReceivedItem(item.Object.Key, context.GetSqsMessage().MessageId!, _scopeMarker.Id));

            if (item.Object.Key == FailOnKey)
            {
                throw new InvalidOperationException("Synthetic nested record failure.");
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed record ReceivedItem(string Key, string SqsMessageId, Guid ScopeId);
}