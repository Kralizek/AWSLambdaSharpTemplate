using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace Kralizek.Lambda;

/// <summary>
/// Returns a text payload unchanged.
/// </summary>
public sealed class PlainTextStringPayloadDecoder : IStringPayloadDecoder<string>
{
    public ValueTask<string> DecodeAsync(string payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(payload);
    }
}

/// <summary>
/// Decodes JSON text payloads using System.Text.Json.
/// </summary>
/// <typeparam name="TPayload">The application contract type produced by the decoder.</typeparam>
public sealed class JsonStringPayloadDecoder<TPayload> : IStringPayloadDecoder<TPayload>
{
    private readonly JsonPayloadDecoderState<TPayload> _state;

    public JsonStringPayloadDecoder()
        : this(JsonSerializerOptions.Default) { }

    public JsonStringPayloadDecoder(JsonSerializerOptions options)
        : this(new JsonPayloadDecoderState<TPayload>(options)) { }

    public JsonStringPayloadDecoder(JsonSerializerContext context)
        : this(JsonPayloadDecoderState<TPayload>.FromContext(context)) { }

    public JsonStringPayloadDecoder(JsonTypeInfo<TPayload> typeInfo)
        : this(new JsonPayloadDecoderState<TPayload>(typeInfo)) { }

    private JsonStringPayloadDecoder(JsonPayloadDecoderState<TPayload> state)
    {
        _state = state;
    }

    public ValueTask<TPayload> DecodeAsync(string payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(_state.Deserialize(payload));
    }
}

/// <summary>
/// Decodes UTF-8 JSON binary payloads using System.Text.Json.
/// </summary>
/// <typeparam name="TPayload">The application contract type produced by the decoder.</typeparam>
public sealed class JsonBinaryPayloadDecoder<TPayload> : IBinaryPayloadDecoder<TPayload>
{
    private readonly JsonPayloadDecoderState<TPayload> _state;

    public JsonBinaryPayloadDecoder()
        : this(JsonSerializerOptions.Default) { }

    public JsonBinaryPayloadDecoder(JsonSerializerOptions options)
        : this(new JsonPayloadDecoderState<TPayload>(options)) { }

    public JsonBinaryPayloadDecoder(JsonSerializerContext context)
        : this(JsonPayloadDecoderState<TPayload>.FromContext(context)) { }

    public JsonBinaryPayloadDecoder(JsonTypeInfo<TPayload> typeInfo)
        : this(new JsonPayloadDecoderState<TPayload>(typeInfo)) { }

    private JsonBinaryPayloadDecoder(JsonPayloadDecoderState<TPayload> state)
    {
        _state = state;
    }

    public ValueTask<TPayload> DecodeAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(_state.Deserialize(payload.Span));
    }
}

internal readonly struct JsonPayloadDecoderState<TPayload>
{
    private readonly JsonSerializerOptions? _options;
    private readonly JsonTypeInfo<TPayload>? _typeInfo;

    public JsonPayloadDecoderState(JsonSerializerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _typeInfo = null;
    }

    public JsonPayloadDecoderState(JsonTypeInfo<TPayload> typeInfo)
    {
        _options = null;
        _typeInfo = typeInfo ?? throw new ArgumentNullException(nameof(typeInfo));
    }

    public static JsonPayloadDecoderState<TPayload> FromContext(JsonSerializerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var typeInfo = context.GetTypeInfo(typeof(TPayload)) as JsonTypeInfo<TPayload>
            ?? throw new ArgumentException(
                $"The serializer context does not contain metadata for {typeof(TPayload).FullName}.",
                nameof(context));

        return new JsonPayloadDecoderState<TPayload>(typeInfo);
    }

    public TPayload Deserialize(string payload)
    {
        TPayload? result;

        if (_typeInfo is not null)
        {
            result = JsonSerializer.Deserialize(payload, _typeInfo);
        }
        else if (_options is not null)
        {
            result = JsonSerializer.Deserialize<TPayload>(payload, _options);
        }
        else
        {
            throw CreateInvalidStateException();
        }

        return EnsureResult(result);
    }

    public TPayload Deserialize(ReadOnlySpan<byte> payload)
    {
        TPayload? result;

        if (_typeInfo is not null)
        {
            result = JsonSerializer.Deserialize(payload, _typeInfo);
        }
        else if (_options is not null)
        {
            result = JsonSerializer.Deserialize<TPayload>(payload, _options);
        }
        else
        {
            throw CreateInvalidStateException();
        }

        return EnsureResult(result);
    }

    private static TPayload EnsureResult(TPayload? result) =>
        result ?? throw new JsonException($"JSON payload deserialized to null for {typeof(TPayload).FullName}.");

    private static InvalidOperationException CreateInvalidStateException() =>
        new("The payload decoder is not configured with JSON serialization metadata.");
}