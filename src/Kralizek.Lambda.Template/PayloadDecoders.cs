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
    private readonly JsonSerializerOptions? _options;
    private readonly JsonTypeInfo<TPayload>? _typeInfo;

    public JsonStringPayloadDecoder()
        : this(JsonSerializerOptions.Default) { }

    public JsonStringPayloadDecoder(JsonSerializerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public JsonStringPayloadDecoder(JsonSerializerContext context)
        : this(GetTypeInfo(context)) { }

    public JsonStringPayloadDecoder(JsonTypeInfo<TPayload> typeInfo)
    {
        _typeInfo = typeInfo ?? throw new ArgumentNullException(nameof(typeInfo));
    }

    public ValueTask<TPayload> DecodeAsync(string payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        cancellationToken.ThrowIfCancellationRequested();

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
            throw new InvalidOperationException("The payload decoder is not configured with JSON serialization metadata.");
        }

        return ValueTask.FromResult(result ?? throw CreateNullPayloadException());
    }

    private static JsonTypeInfo<TPayload> GetTypeInfo(JsonSerializerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.GetTypeInfo(typeof(TPayload)) as JsonTypeInfo<TPayload>
            ?? throw new ArgumentException(
                $"The serializer context does not contain metadata for {typeof(TPayload).FullName}.",
                nameof(context));
    }

    private static JsonException CreateNullPayloadException() =>
        new($"JSON payload deserialized to null for {typeof(TPayload).FullName}.");
}

/// <summary>
/// Decodes UTF-8 JSON binary payloads using System.Text.Json.
/// </summary>
/// <typeparam name="TPayload">The application contract type produced by the decoder.</typeparam>
public sealed class JsonBinaryPayloadDecoder<TPayload> : IBinaryPayloadDecoder<TPayload>
{
    private readonly JsonSerializerOptions? _options;
    private readonly JsonTypeInfo<TPayload>? _typeInfo;

    public JsonBinaryPayloadDecoder()
        : this(JsonSerializerOptions.Default) { }

    public JsonBinaryPayloadDecoder(JsonSerializerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public JsonBinaryPayloadDecoder(JsonSerializerContext context)
        : this(GetTypeInfo(context)) { }

    public JsonBinaryPayloadDecoder(JsonTypeInfo<TPayload> typeInfo)
    {
        _typeInfo = typeInfo ?? throw new ArgumentNullException(nameof(typeInfo));
    }

    public ValueTask<TPayload> DecodeAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        TPayload? result;

        if (_typeInfo is not null)
        {
            result = JsonSerializer.Deserialize(payload.Span, _typeInfo);
        }
        else if (_options is not null)
        {
            result = JsonSerializer.Deserialize<TPayload>(payload.Span, _options);
        }
        else
        {
            throw new InvalidOperationException("The payload decoder is not configured with JSON serialization metadata.");
        }

        return ValueTask.FromResult(result ?? throw CreateNullPayloadException());
    }

    private static JsonTypeInfo<TPayload> GetTypeInfo(JsonSerializerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.GetTypeInfo(typeof(TPayload)) as JsonTypeInfo<TPayload>
            ?? throw new ArgumentException(
                $"The serializer context does not contain metadata for {typeof(TPayload).FullName}.",
                nameof(context));
    }

    private static JsonException CreateNullPayloadException() =>
        new($"JSON payload deserialized to null for {typeof(TPayload).FullName}.");
}