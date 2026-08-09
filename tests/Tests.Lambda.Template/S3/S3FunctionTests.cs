using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.S3Events;

using Kralizek.Lambda;

using NUnit.Framework;

namespace Tests.Lambda.S3;

[TestFixture]
public class S3FunctionTests
{
    [SetUp]
    public void SetUp() => NotificationHandler.Reset();

    [Test]
    public async Task Function_maps_notification_to_synthetic_event()
    {
        var record = new S3Event.S3EventNotificationRecord
        {
            EventName = "ObjectCreated:Put",
            EventTime = new DateTime(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc),
            S3 = new S3Event.S3Entity
            {
                Bucket = new S3Event.S3BucketEntity { Name = "uploads" },
                Object = new S3Event.S3ObjectEntity
                {
                    Key = "folder%2Fhello+world.txt",
                    VersionId = "v1",
                    Sequencer = "00ABC"
                }
            }
        };

        await new NotificationFunction().FunctionHandlerAsync(
            new S3Event { Records = new List<S3Event.S3EventNotificationRecord> { record } },
            TestLambdaContexts.Create());

        var item = NotificationHandler.Items.Single();

        Assert.Multiple(() =>
        {
            Assert.That(item.Object.Bucket, Is.EqualTo("uploads"));
            Assert.That(item.Object.Key, Is.EqualTo("folder/hello world.txt"));
            Assert.That(item.Object.VersionId, Is.EqualTo("v1"));
            Assert.That(item.EventName, Is.EqualTo(S3EventName.ObjectCreatedPut));
            Assert.That(item.EventName.IsObjectCreated, Is.True);
            Assert.That(item.Sequencer, Is.EqualTo("00ABC"));
            Assert.That(NotificationHandler.Context?.GetS3EventRecord(), Is.SameAs(record));
            Assert.That(S3RecordResult.Completed.Value, Is.SameAs(S3RecordResult.Completed));
        });
    }

    [Test]
    public async Task Batch_function_maps_schema_2_task_and_response()
    {
        BatchHandler.Reset();

        var task = new S3BatchTask
        {
            TaskId = "task-1",
            S3Bucket = "uploads",
            S3Key = "folder%2Fhello+world.txt",
            S3VersionId = "v1"
        };
        var request = new S3BatchEvent
        {
            InvocationSchemaVersion = "2.0",
            InvocationId = "invocation-1",
            Job = new S3BatchJob
            {
                Id = "job-1",
                UserArguments = new Dictionary<string, string> { ["mode"] = "validate" }
            },
            Tasks = new List<S3BatchTask> { task }
        };

        var response = await new BatchFunction().FunctionHandlerAsync(request, TestLambdaContexts.Create());
        var item = BatchHandler.Items.Single();
        var objectKey = item.Key as S3BatchObjectKey;
        var batchResult = S3BatchResult.Succeeded();

        Assert.Multiple(() =>
        {
            Assert.That(objectKey, Is.Not.Null);
            Assert.That(objectKey!.Object.Bucket, Is.EqualTo("uploads"));
            Assert.That(objectKey.Object.Key, Is.EqualTo("folder/hello world.txt"));
            Assert.That(response.InvocationSchemaVersion, Is.EqualTo("2.0"));
            Assert.That(response.InvocationId, Is.EqualTo("invocation-1"));
            Assert.That(response.TreatMissingKeysAs, Is.EqualTo("TemporaryFailure"));
            Assert.That(response.Results.Single().TaskId, Is.EqualTo("task-1"));
            Assert.That(response.Results.Single().ResultCode, Is.EqualTo("Succeeded"));
            Assert.That(BatchHandler.Context?.InvocationId, Is.EqualTo("invocation-1"));
            Assert.That(BatchHandler.Context?.JobId, Is.EqualTo("job-1"));
            Assert.That(BatchHandler.Context?.TaskId, Is.EqualTo("task-1"));
            Assert.That(BatchHandler.Context?.UserArguments["mode"], Is.EqualTo("validate"));
            Assert.That(BatchHandler.Context?.GetS3BatchRequest(), Is.SameAs(request));
            Assert.That(BatchHandler.Context?.GetS3BatchTask(), Is.SameAs(task));
            Assert.That(batchResult.Value, Is.SameAs(batchResult));
        });
    }

    [Test]
    public void Batch_function_rejects_schema_1()
    {
        var request = new S3BatchEvent
        {
            InvocationSchemaVersion = "1.0",
            InvocationId = "invocation-1",
            Tasks = new List<S3BatchTask> { new() { TaskId = "task-1" } }
        };

        Assert.ThrowsAsync<NotSupportedException>(
            async () => await new BatchFunction().FunctionHandlerAsync(request, TestLambdaContexts.Create()));
    }

    private sealed class NotificationFunction : S3Function<NotificationHandler>;
    private sealed class BatchFunction : S3BatchFunction<BatchHandler>;

    private sealed class NotificationHandler : IS3ObjectEventHandler
    {
        private static readonly List<S3ObjectEvent> Received = new();

        public static IReadOnlyCollection<S3ObjectEvent> Items => Received;
        public static S3RecordContext? Context { get; private set; }

        public static void Reset()
        {
            Received.Clear();
            Context = null;
        }

        public ValueTask HandleAsync(S3ObjectEvent item, S3RecordContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Received.Add(item);
            Context = context;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class BatchHandler : IS3BatchItemHandler
    {
        private static readonly List<S3BatchItem> Received = new();

        public static IReadOnlyCollection<S3BatchItem> Items => Received;
        public static S3BatchContext? Context { get; private set; }

        public static void Reset()
        {
            Received.Clear();
            Context = null;
        }

        public ValueTask<S3BatchResult> HandleAsync(
            S3BatchItem item,
            S3BatchContext context,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Received.Add(item);
            Context = context;
            return ValueTask.FromResult(S3BatchResult.Succeeded());
        }
    }
}