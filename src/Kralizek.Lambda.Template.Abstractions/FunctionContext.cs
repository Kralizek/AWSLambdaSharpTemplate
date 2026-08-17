using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Kralizek.Lambda;

/// <summary>
/// Contains the source-neutral metadata exposed for a Lambda invocation.
/// </summary>
public sealed record FunctionContextMetadata(
    string AwsRequestId,
    string FunctionName,
    string FunctionVersion,
    string InvokedFunctionArn,
    int MemoryLimitInMB,
    TimeSpan RemainingTime,
    string LogGroupName,
    string LogStreamName);

/// <summary>
/// Provides metadata about the current function invocation without depending on a source-specific runtime context.
/// </summary>
public abstract class FunctionContext
{
    protected FunctionContext(
        FunctionContextMetadata metadata,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        AwsRequestId = metadata.AwsRequestId;
        FunctionName = metadata.FunctionName;
        FunctionVersion = metadata.FunctionVersion;
        InvokedFunctionArn = metadata.InvokedFunctionArn;
        MemoryLimitInMB = metadata.MemoryLimitInMB;
        RemainingTime = metadata.RemainingTime;
        LogGroupName = metadata.LogGroupName;
        LogStreamName = metadata.LogStreamName;

        var propertySnapshot = properties is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(properties);

        Properties = new ReadOnlyDictionary<string, object?>(propertySnapshot);
    }

    protected FunctionContext(
        FunctionContext source,
        string propertyName,
        object? propertyValue)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        AwsRequestId = source.AwsRequestId;
        FunctionName = source.FunctionName;
        FunctionVersion = source.FunctionVersion;
        InvokedFunctionArn = source.InvokedFunctionArn;
        MemoryLimitInMB = source.MemoryLimitInMB;
        RemainingTime = source.RemainingTime;
        LogGroupName = source.LogGroupName;
        LogStreamName = source.LogStreamName;
        Properties = new PropertyOverlay(source.Properties, propertyName, propertyValue);
    }

    public string AwsRequestId { get; }

    public string FunctionName { get; }

    public string FunctionVersion { get; }

    public string InvokedFunctionArn { get; }

    public int MemoryLimitInMB { get; }

    public TimeSpan RemainingTime { get; }

    public string LogGroupName { get; }

    public string LogStreamName { get; }

    /// <summary>
    /// Gets additional runtime-specific data that is not represented by the strongly typed properties.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Properties { get; }

#pragma warning disable S3267 // Explicit loops avoid LINQ iterator allocations on this per-record hot path.
    private sealed class PropertyOverlay(
        IReadOnlyDictionary<string, object?> source,
        string propertyName,
        object? propertyValue) : IReadOnlyDictionary<string, object?>
    {
        public int Count => source.ContainsKey(propertyName) ? source.Count : source.Count + 1;

        public IEnumerable<string> Keys
        {
            get
            {
                foreach (var key in source.Keys)
                {
                    if (!string.Equals(key, propertyName, StringComparison.Ordinal))
                    {
                        yield return key;
                    }
                }

                yield return propertyName;
            }
        }

        public IEnumerable<object?> Values
        {
            get
            {
                foreach (var pair in source)
                {
                    if (!string.Equals(pair.Key, propertyName, StringComparison.Ordinal))
                    {
                        yield return pair.Value;
                    }
                }

                yield return propertyValue;
            }
        }

        public object? this[string key]
            => string.Equals(key, propertyName, StringComparison.Ordinal)
                ? propertyValue
                : source[key];

        public bool ContainsKey(string key)
            => string.Equals(key, propertyName, StringComparison.Ordinal) || source.ContainsKey(key);

        public bool TryGetValue(string key, out object? value)
        {
            if (string.Equals(key, propertyName, StringComparison.Ordinal))
            {
                value = propertyValue;
                return true;
            }

            return source.TryGetValue(key, out value);
        }

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            foreach (var pair in source)
            {
                if (!string.Equals(pair.Key, propertyName, StringComparison.Ordinal))
                {
                    yield return pair;
                }
            }

            yield return new KeyValuePair<string, object?>(propertyName, propertyValue);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
#pragma warning restore S3267
}

/// <summary>
/// Invocation context for completion-only event functions and source-specific event contexts.
/// </summary>
public class EventContext : FunctionContext
{
    protected EventContext(
        FunctionContextMetadata metadata,
        IReadOnlyDictionary<string, object?>? properties = null)
        : base(metadata, properties) { }
}

/// <summary>
/// Invocation context for request/response functions and source-specific request contexts.
/// </summary>
public class RequestContext : FunctionContext
{
    protected RequestContext(
        FunctionContextMetadata metadata,
        IReadOnlyDictionary<string, object?>? properties = null)
        : base(metadata, properties) { }
}

/// <summary>
/// Invocation context shared by record-oriented functions and source-specific contexts.
/// </summary>
public class RecordContext : FunctionContext
{
    protected RecordContext(
        FunctionContextMetadata metadata,
        IReadOnlyDictionary<string, object?>? properties = null)
        : base(metadata, properties) { }

    protected RecordContext(
        RecordContext source,
        string propertyName,
        object? propertyValue)
        : base(source, propertyName, propertyValue) { }
}
