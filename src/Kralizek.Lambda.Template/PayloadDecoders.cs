using System;
using System.Diagnostics.CodeAnalysis;
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
    private readonly Func<string, TPayload> _deserialize;

    [RequiresDynamicCode("Reflection-based JSON serialization is not compatible with Native AOT. Use a JsonSerializerContext or JsonTypeInfo<TPayload> constructor instead.")]
    [RequiresUnreferencedCode("Reflection-based JSON serialization is not compatible with trimming. Use a JsonSerializerContext or JsonTypeInfo<TPayload> constructor instead.")]
    public JsonStringPayloadDecoder()
        : this(new JsonSerializerOptions(JsonSerializerDefaults.Web)) { }

    [RequiresDynamicCode("Reflection-based JSON serialization is not compatible with Native AOT. Use a JsonSerializerContext or JsonTypeInfo<TPayload> constructor instead.")]
    [RequiresUnreferencedCode("Reflection-based JSON serialization is not compatible with trimming. Use a JsonSerializerContext or JsonTypeInfo<TPayload> constructor instead.")]
    public JsonStringPayloadDecoder(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _deserialize = payload => EnsureResult(JsonSerializer.Deserialize<TPayload>(payload, options));
    }

    public JsonStringPayloadDecoder(JsonSerializerContext context)
        : this(GetTypeInfo(context)) { }

    public JsonStringPayloadDecoder(JsonTypeInfo<TPayload> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        _deserialize = payload => EnsureResult(JsonSerializer.Deserialize(payload, typeInfo));
    }

    public ValueTask<TPayload> DecodeAsync(string payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(_deserialize(payload));
    }

    private static JsonTypeInfo<TPayload> GetTypeInfo(JsonSerializerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.GetTypeInfo(typeof(TPayload)) as JsonTypeInfo<TPayload>
            ?? throw new ArgumentException(
                $"The serializer context does not contain metadata for {typeof(TPayload).FullName}.",
                nameof(context));
    }

    private static TPayload EnsureResult(TPayload? result) =>
        result ?? throw new JsonException($"JSON payload deserialized to null for {typeof(TPayload).FullName}.");
}

/// <summary>
/// Decodes UTF-8 JSON binary payloads using System.Text.Json.
/// </summary>
/// <typeparam name="TPayload">The application contract type produced by the decoder.</typeparam>
public sealed class JsonBinaryPayloadDecoder<TPayload> : IBinaryPayloadDecoder<TPayload>
{
    private delegate TPayload DeserializeDelegate(ReadOnlySpan<byte> payload);

    private readonly DeserializeDelegate _deserialize;

    [RequiresDynamicCode("Reflection-based JSON serialization is not compatible with Native AOT. Use a JsonSerializerContext or JsonTypeInfo<TPayload> constructor instead.")]
    [RequiresUnreferencedCode("Reflection-based JSON serialization is not compatible with trimming. Use a JsonSerializerContext or JsonTypeInfo<TPayload> constructor instead.")]
    public JsonBinaryPayloadDecoder()
        : this(new JsonSerializerOptions(JsonSerializerDefaults.Web)) { }

    [RequiresDynamicCode("Reflection-based JSON serialization is not compatible with Native AOT. Use a JsonSerializerContext or JsonTypeInfo<TPayload> constructor instead.")]
    [RequiresUnreferencedCode("Reflection-based JSON serialization is not compatible with trimming. Use a JsonSerializerContext or JsonTypeInfo<TPayload> constructor instead.")]
    public JsonBinaryPayloadDecoder(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _deserialize = payload => EnsureResult(JsonSerializer.Deserialize<TPayload>(payload, options));
    }

    public JsonBinaryPayloadDecoder(JsonSerializerContext context)
        : this(GetTypeInfo(context)) { }

    public JsonBinaryPayloadDecoder(JsonTypeInfo<TPayload> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        _deserialize = payload => EnsureResult(JsonSerializer.Deserialize(payload, typeInfo));
    }

    public ValueTask<TPayload> DecodeAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(_deserialize(payload.Span));
    }

    private static JsonTypeInfo<TPayload> GetTypeInfo(JsonSerializerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.GetTypeInfo(typeof(TPayload)) as JsonTypeInfo<TPayload>
            ?? throw new ArgumentException(
                $"The serializer context does not contain metadata for {typeof(TPayload).FullName}.",
                nameof(context));
    }

    private static TPayload EnsureResult(TPayload? result) =>
        result ?? throw new JsonException($"JSON payload deserialized to null for {typeof(TPayload).FullName}.");
}
