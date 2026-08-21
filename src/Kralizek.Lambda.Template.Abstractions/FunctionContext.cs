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
        : this(metadata, CreatePropertySnapshot(properties))
    {
    }

    protected FunctionContext(
        FunctionContextMetadata metadata,
        string propertyName,
        object? propertyValue)
        : this(metadata, new SinglePropertyDictionary(propertyName, propertyValue))
    {
    }

    protected FunctionContext(
        FunctionContext source,
        string propertyName,
        object? propertyValue)
        : this(source, CreatePropertyOverlay(source, propertyName, propertyValue))
    {
    }

    protected FunctionContext(
        FunctionContext source,
        string firstPropertyName,
        object? firstPropertyValue,
        string secondPropertyName,
        object? secondPropertyValue)
        : this(
            source,
            CreatePropertyOverlay(
                source,
                firstPropertyName,
                firstPropertyValue,
                secondPropertyName,
                secondPropertyValue))
    {
    }

    private FunctionContext(
        FunctionContextMetadata metadata,
        IReadOnlyDictionary<string, object?> properties)
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
        Properties = properties;
    }

    private FunctionContext(FunctionContext source, IReadOnlyDictionary<string, object?> properties)
    {
        ArgumentNullException.ThrowIfNull(source);

        AwsRequestId = source.AwsRequestId;
        FunctionName = source.FunctionName;
        FunctionVersion = source.FunctionVersion;
        InvokedFunctionArn = source.InvokedFunctionArn;
        MemoryLimitInMB = source.MemoryLimitInMB;
        RemainingTime = source.RemainingTime;
        LogGroupName = source.LogGroupName;
        LogStreamName = source.LogStreamName;
        Properties = properties;
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

    private static IReadOnlyDictionary<string, object?> CreatePropertySnapshot(
        IReadOnlyDictionary<string, object?>? properties)
    {
        var propertySnapshot = properties is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(properties);

        return new ReadOnlyDictionary<string, object?>(propertySnapshot);
    }

    private static IReadOnlyDictionary<string, object?> CreatePropertyOverlay(
        FunctionContext source,
        string propertyName,
        object? propertyValue)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        return new PropertyOverlay(source.Properties, propertyName, propertyValue);
    }

    private static IReadOnlyDictionary<string, object?> CreatePropertyOverlay(
        FunctionContext source,
        string firstPropertyName,
        object? firstPropertyValue,
        string secondPropertyName,
        object? secondPropertyValue)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrEmpty(firstPropertyName);
        ArgumentException.ThrowIfNullOrEmpty(secondPropertyName);

        return new PropertyOverlay(
            source.Properties,
            firstPropertyName,
            firstPropertyValue,
            secondPropertyName,
            secondPropertyValue);
    }

    private sealed class SinglePropertyDictionary : IReadOnlyDictionary<string, object?>
    {
        private readonly string _propertyName;
        private readonly object? _propertyValue;

        public SinglePropertyDictionary(string propertyName, object? propertyValue)
        {
            ArgumentException.ThrowIfNullOrEmpty(propertyName);

            _propertyName = propertyName;
            _propertyValue = propertyValue;
        }

        public int Count => 1;

        public IEnumerable<string> Keys
        {
            get
            {
                yield return _propertyName;
            }
        }

        public IEnumerable<object?> Values
        {
            get
            {
                yield return _propertyValue;
            }
        }

        public object? this[string key]
            => string.Equals(key, _propertyName, StringComparison.Ordinal)
                ? _propertyValue
                : throw new KeyNotFoundException();

        public bool ContainsKey(string key) => string.Equals(key, _propertyName, StringComparison.Ordinal);

        public bool TryGetValue(string key, out object? value)
        {
            if (string.Equals(key, _propertyName, StringComparison.Ordinal))
            {
                value = _propertyValue;
                return true;
            }

            value = null;
            return false;
        }

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            yield return new KeyValuePair<string, object?>(_propertyName, _propertyValue);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

#pragma warning disable S3267 // Explicit loops avoid LINQ iterator allocations on this per-record hot path.
    private sealed class PropertyOverlay : IReadOnlyDictionary<string, object?>
    {
        private readonly IReadOnlyDictionary<string, object?> _source;
        private readonly string _firstPropertyName;
        private readonly object? _firstPropertyValue;
        private readonly string? _secondPropertyName;
        private readonly object? _secondPropertyValue;

        public PropertyOverlay(
            IReadOnlyDictionary<string, object?> source,
            string propertyName,
            object? propertyValue)
        {
            _source = source;
            _firstPropertyName = propertyName;
            _firstPropertyValue = propertyValue;
        }

        public PropertyOverlay(
            IReadOnlyDictionary<string, object?> source,
            string firstPropertyName,
            object? firstPropertyValue,
            string secondPropertyName,
            object? secondPropertyValue)
        {
            _source = source;
            _firstPropertyName = firstPropertyName;
            _firstPropertyValue = firstPropertyValue;
            _secondPropertyName = secondPropertyName;
            _secondPropertyValue = secondPropertyValue;
        }

        public int Count
        {
            get
            {
                var count = _source.Count;

                if (!_source.ContainsKey(_firstPropertyName))
                {
                    count++;
                }

                if (_secondPropertyName is not null
                    && !string.Equals(_secondPropertyName, _firstPropertyName, StringComparison.Ordinal)
                    && !_source.ContainsKey(_secondPropertyName))
                {
                    count++;
                }

                return count;
            }
        }

        public IEnumerable<string> Keys
        {
            get
            {
                foreach (var key in _source.Keys)
                {
                    if (!IsOverlayKey(key))
                    {
                        yield return key;
                    }
                }

                yield return _firstPropertyName;

                if (_secondPropertyName is not null
                    && !string.Equals(_secondPropertyName, _firstPropertyName, StringComparison.Ordinal))
                {
                    yield return _secondPropertyName;
                }
            }
        }

        public IEnumerable<object?> Values
        {
            get
            {
                foreach (var pair in _source)
                {
                    if (!IsOverlayKey(pair.Key))
                    {
                        yield return pair.Value;
                    }
                }

                yield return GetOverlayValue(_firstPropertyName);

                if (_secondPropertyName is not null
                    && !string.Equals(_secondPropertyName, _firstPropertyName, StringComparison.Ordinal))
                {
                    yield return _secondPropertyValue;
                }
            }
        }

        public object? this[string key]
        {
            get
            {
                if (TryGetOverlayValue(key, out var value))
                {
                    return value;
                }

                return _source[key];
            }
        }

        public bool ContainsKey(string key) => IsOverlayKey(key) || _source.ContainsKey(key);

        public bool TryGetValue(string key, out object? value)
        {
            if (TryGetOverlayValue(key, out value))
            {
                return true;
            }

            return _source.TryGetValue(key, out value);
        }

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            foreach (var pair in _source)
            {
                if (!IsOverlayKey(pair.Key))
                {
                    yield return pair;
                }
            }

            yield return new KeyValuePair<string, object?>(_firstPropertyName, GetOverlayValue(_firstPropertyName));

            if (_secondPropertyName is not null
                && !string.Equals(_secondPropertyName, _firstPropertyName, StringComparison.Ordinal))
            {
                yield return new KeyValuePair<string, object?>(_secondPropertyName, _secondPropertyValue);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private bool IsOverlayKey(string key)
            => string.Equals(key, _firstPropertyName, StringComparison.Ordinal)
                || (_secondPropertyName is not null
                    && string.Equals(key, _secondPropertyName, StringComparison.Ordinal));

        private object? GetOverlayValue(string key)
            => _secondPropertyName is not null
                && string.Equals(key, _secondPropertyName, StringComparison.Ordinal)
                    ? _secondPropertyValue
                    : _firstPropertyValue;

        private bool TryGetOverlayValue(string key, out object? value)
        {
            if (_secondPropertyName is not null
                && string.Equals(key, _secondPropertyName, StringComparison.Ordinal))
            {
                value = _secondPropertyValue;
                return true;
            }

            if (string.Equals(key, _firstPropertyName, StringComparison.Ordinal))
            {
                value = _firstPropertyValue;
                return true;
            }

            value = null;
            return false;
        }
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

    protected EventContext(
        FunctionContextMetadata metadata,
        string propertyName,
        object? propertyValue)
        : base(metadata, propertyName, propertyValue) { }
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

    protected RequestContext(
        FunctionContextMetadata metadata,
        string propertyName,
        object? propertyValue)
        : base(metadata, propertyName, propertyValue) { }
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
        FunctionContextMetadata metadata,
        string propertyName,
        object? propertyValue)
        : base(metadata, propertyName, propertyValue) { }

    protected RecordContext(
        RecordContext source,
        string propertyName,
        object? propertyValue)
        : base(source, propertyName, propertyValue) { }

    protected RecordContext(
        RecordContext source,
        string firstPropertyName,
        object? firstPropertyValue,
        string secondPropertyName,
        object? secondPropertyValue)
        : base(
            source,
            firstPropertyName,
            firstPropertyValue,
            secondPropertyName,
            secondPropertyValue)
    { }
}
