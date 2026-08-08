using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Kralizek.Lambda;

using NUnit.Framework;

namespace Tests.Lambda;

[TestFixture]
public class PayloadDecoderTests
{
    [Test]
    public async Task Plain_text_decoder_returns_payload_unchanged()
    {
        var decoder = new PlainTextStringPayloadDecoder();

        var result = await decoder.DecodeAsync("hello");

        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public async Task String_decoder_uses_web_json_defaults()
    {
        var decoder = new JsonStringPayloadDecoder<TestPayload>();

        var result = await decoder.DecodeAsync("{\"value\":\"hello\"}");

        Assert.That(result.Value, Is.EqualTo("hello"));
    }

    [Test]
    public async Task Binary_decoder_uses_web_json_defaults()
    {
        var decoder = new JsonBinaryPayloadDecoder<TestPayload>();
        var payload = Encoding.UTF8.GetBytes("{\"value\":\"hello\"}");

        var result = await decoder.DecodeAsync(payload);

        Assert.That(result.Value, Is.EqualTo("hello"));
    }

    [Test]
    public async Task String_decoder_uses_supplied_serializer_options()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var decoder = new JsonStringPayloadDecoder<TestPayload>(options);

        var result = await decoder.DecodeAsync("{\"value\":\"hello\"}");

        Assert.That(result.Value, Is.EqualTo("hello"));
    }

    [Test]
    public async Task String_decoder_uses_source_generated_serializer_context()
    {
        var decoder = new JsonStringPayloadDecoder<TestPayload>(PayloadDecoderJsonContext.Default);

        var result = await decoder.DecodeAsync("{\"Value\":\"hello\"}");

        Assert.That(result.Value, Is.EqualTo("hello"));
    }

    [Test]
    public async Task Binary_decoder_uses_source_generated_serializer_context()
    {
        var decoder = new JsonBinaryPayloadDecoder<TestPayload>(PayloadDecoderJsonContext.Default);

        var result = await decoder.DecodeAsync("{\"Value\":\"hello\"}"u8.ToArray());

        Assert.That(result.Value, Is.EqualTo("hello"));
    }

    [Test]
    public void Decoders_honor_pre_cancelled_tokens()
    {
        var plainTextDecoder = new PlainTextStringPayloadDecoder();
        var stringDecoder = new JsonStringPayloadDecoder<TestPayload>();
        var binaryDecoder = new JsonBinaryPayloadDecoder<TestPayload>();
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(
            async () => await plainTextDecoder.DecodeAsync("hello", cancellationTokenSource.Token));
        Assert.ThrowsAsync<OperationCanceledException>(
            async () => await stringDecoder.DecodeAsync("{}", cancellationTokenSource.Token));
        Assert.ThrowsAsync<OperationCanceledException>(
            async () => await binaryDecoder.DecodeAsync("{}"u8.ToArray(), cancellationTokenSource.Token));
    }
}

internal sealed record TestPayload(string Value);

[JsonSerializable(typeof(TestPayload))]
internal partial class PayloadDecoderJsonContext : JsonSerializerContext
{
}