using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Kralizek.Lambda;

/// <summary>
/// Decodes JSON text payloads using System.Text.Json.
/// </summary>
/// <typeparam name="TPayload">The application contract type produced by the decoder.</typeparam>
public sealed class JsonStringPayloadDecoder<TPayload> : IStringPayloadDecoder<TPayload>
{
    private readonly JsonSerializerOptions _options;

    public JsonStringPayloadDecoder()
        : this(JsonSerializerOptions.Default) { }

    public JsonStringPayloadDecoder(JsonSerializerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public ValueTask<TPayload> DecodeAsync(string payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        cancellationToken.ThrowIfCancellationRequested();

        var result = JsonSerializer.Deserialize<TPayload>(payload, _options);

        return ValueTask.FromResult(result ?? throw CreateNullPayloadException());
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
    private readonly JsonSerializerOptions _options;

    public JsonBinaryPayloadDecoder()
        : this(JsonSerializerOptions.Default) { }

    public JsonBinaryPayloadDecoder(JsonSerializerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public ValueTask<TPayload> DecodeAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = JsonSerializer.Deserialize<TPayload>(payload.Span, _options);

        return ValueTask.FromResult(result ?? throw CreateNullPayloadException());
    }

    private static JsonException CreateNullPayloadException() =>
        new($"JSON payload deserialized to null for {typeof(TPayload).FullName}.");
}