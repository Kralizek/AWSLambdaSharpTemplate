using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Text.Json.Serialization;

using Amazon.Lambda.S3Events;

namespace Kralizek.Lambda;

/// <summary>
/// Identifies an object stored in Amazon S3.
/// </summary>
public sealed record S3ObjectReference(string Bucket, string Key, string? VersionId);

/// <summary>
/// Represents an Amazon S3 event notification name while preserving forward compatibility.
/// </summary>
public sealed record S3EventName(string Value)
{
    public static readonly S3EventName ObjectCreatedPut = new("ObjectCreated:Put");
    public static readonly S3EventName ObjectCreatedPost = new("ObjectCreated:Post");
    public static readonly S3EventName ObjectCreatedCopy = new("ObjectCreated:Copy");
    public static readonly S3EventName ObjectCreatedCompleteMultipartUpload = new("ObjectCreated:CompleteMultipartUpload");
    public static readonly S3EventName ObjectRemovedDelete = new("ObjectRemoved:Delete");
    public static readonly S3EventName ObjectRemovedDeleteMarkerCreated = new("ObjectRemoved:DeleteMarkerCreated");
    public static readonly S3EventName ObjectRestorePost = new("ObjectRestore:Post");
    public static readonly S3EventName ObjectRestoreCompleted = new("ObjectRestore:Completed");
    public static readonly S3EventName ObjectRestoreDelete = new("ObjectRestore:Delete");
    public static readonly S3EventName ReducedRedundancyLostObject = new("ReducedRedundancyLostObject");
    public static readonly S3EventName ReplicationOperationFailedReplication = new("Replication:OperationFailedReplication");
    public static readonly S3EventName ReplicationOperationMissedThreshold = new("Replication:OperationMissedThreshold");
    public static readonly S3EventName ReplicationOperationReplicatedAfterThreshold = new("Replication:OperationReplicatedAfterThreshold");
    public static readonly S3EventName ReplicationOperationNotTracked = new("Replication:OperationNotTracked");
    public static readonly S3EventName LifecycleExpirationDelete = new("LifecycleExpiration:Delete");
    public static readonly S3EventName LifecycleExpirationDeleteMarkerCreated = new("LifecycleExpiration:DeleteMarkerCreated");
    public static readonly S3EventName LifecycleTransition = new("LifecycleTransition");
    public static readonly S3EventName IntelligentTiering = new("IntelligentTiering");
    public static readonly S3EventName ObjectTaggingPut = new("ObjectTagging:Put");
    public static readonly S3EventName ObjectTaggingDelete = new("ObjectTagging:Delete");
    public static readonly S3EventName ObjectAnnotationPut = new("ObjectAnnotation:Put");
    public static readonly S3EventName ObjectAnnotationDelete = new("ObjectAnnotation:Delete");
    public static readonly S3EventName ObjectAclPut = new("ObjectAcl:Put");

    public bool IsObjectCreated => IsFamily("ObjectCreated:");
    public bool IsObjectRemoved => IsFamily("ObjectRemoved:");
    public bool IsObjectRestore => IsFamily("ObjectRestore:");
    public bool IsReplication => IsFamily("Replication:");
    public bool IsLifecycleExpiration => IsFamily("LifecycleExpiration:");
    public bool IsObjectTagging => IsFamily("ObjectTagging:");
    public bool IsObjectAnnotation => IsFamily("ObjectAnnotation:");

    public static implicit operator S3EventName(string value) => new(value);

    public override string ToString() => Value;

    private bool IsFamily(string prefix) => Value.StartsWith(prefix, StringComparison.Ordinal);
}

/// <summary>
/// Represents an event that occurred for an Amazon S3 object.
/// </summary>
public sealed record S3ObjectEvent(
    S3ObjectReference Object,
    S3EventName EventName,
    DateTimeOffset EventTime,
    string? Sequencer)
{
    internal static S3ObjectEvent Create(S3Event.S3EventNotificationRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var entity = record.S3 ?? throw new InvalidOperationException("The S3 event record does not contain S3 information.");
        var s3Object = entity.Object ?? throw new InvalidOperationException("The S3 event record does not contain object information.");
        var bucket = entity.Bucket?.Name ?? throw new InvalidOperationException("The S3 event record does not contain a bucket name.");

        return new S3ObjectEvent(
            new S3ObjectReference(bucket, s3Object.KeyDecoded, s3Object.VersionId),
            new S3EventName(record.EventName ?? string.Empty),
            new DateTimeOffset(record.EventTime),
            s3Object.Sequencer);
    }
}

/// <summary>
/// Base type for keys carried by S3 Batch Operations tasks.
/// </summary>
public abstract record S3BatchTaskKey;

/// <summary>
/// An S3 Batch Operations key referring to an S3 object.
/// </summary>
public sealed record S3BatchObjectKey(S3ObjectReference Object) : S3BatchTaskKey;

/// <summary>
/// Represents one S3 Batch Operations work item.
/// </summary>
public sealed record S3BatchItem(S3BatchTaskKey Key)
{
    internal static S3BatchItem Create(S3BatchTask task)
    {
        ArgumentNullException.ThrowIfNull(task);

        var bucket = task.S3Bucket ?? throw new InvalidOperationException("The S3 Batch task does not contain a bucket name.");
        var key = task.S3Key ?? throw new InvalidOperationException("The S3 Batch task does not contain an object key.");

        return new S3BatchItem(
            new S3BatchObjectKey(
                new S3ObjectReference(bucket, WebUtility.UrlDecode(key), task.S3VersionId)));
    }
}

public enum S3BatchResultCode
{
    Succeeded,
    TemporaryFailure,
    PermanentFailure
}

public sealed record S3BatchResult(S3BatchResultCode Code, string? Message = null)
{
    public static S3BatchResult Succeeded(string? message = null) => new(S3BatchResultCode.Succeeded, message);
    public static S3BatchResult TemporaryFailure(string? message = null) => new(S3BatchResultCode.TemporaryFailure, message);
    public static S3BatchResult PermanentFailure(string? message = null) => new(S3BatchResultCode.PermanentFailure, message);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class S3BatchEvent
{
    [JsonPropertyName("invocationSchemaVersion")]
    public string? InvocationSchemaVersion { get; set; }

    [JsonPropertyName("invocationId")]
    public string? InvocationId { get; set; }

    [JsonPropertyName("job")]
    public S3BatchJob? Job { get; set; }

    [JsonPropertyName("tasks")]
    public List<S3BatchTask>? Tasks { get; set; }
}

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class S3BatchJob
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("userArguments")]
    public Dictionary<string, string>? UserArguments { get; set; }
}

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class S3BatchTask
{
    [JsonPropertyName("taskId")]
    public string? TaskId { get; set; }

    [JsonPropertyName("s3Bucket")]
    public string? S3Bucket { get; set; }

    [JsonPropertyName("s3Key")]
    public string? S3Key { get; set; }

    [JsonPropertyName("s3VersionId")]
    public string? S3VersionId { get; set; }

    [JsonIgnore]
    internal S3BatchEvent? Request { get; set; }
}

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class S3BatchResponse
{
    [JsonPropertyName("invocationSchemaVersion")]
    public string InvocationSchemaVersion { get; set; } = "2.0";

    [JsonPropertyName("treatMissingKeysAs")]
    public string TreatMissingKeysAs { get; set; } = nameof(S3BatchResultCode.TemporaryFailure);

    [JsonPropertyName("invocationId")]
    public string? InvocationId { get; set; }

    [JsonPropertyName("results")]
    public List<S3BatchTaskResponse> Results { get; set; } = new();
}

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class S3BatchTaskResponse
{
    [JsonPropertyName("taskId")]
    public string? TaskId { get; set; }

    [JsonPropertyName("resultCode")]
    public string? ResultCode { get; set; }

    [JsonPropertyName("resultString")]
    public string? ResultString { get; set; }
}