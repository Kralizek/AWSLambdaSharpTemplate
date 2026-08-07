using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Kralizek.Lambda;

using NUnit.Framework;

namespace Tests.Lambda;

[TestFixture]
public class PayloadDecoderTests
{
    [Test]
    public async Task String_decoder_deserializes_json_payload()
    {
        var decoder = new JsonStringPayloadDecoder<TestPayload>();

        var result = await decoder.DecodeAsync("{\"Value\":\"hello\"}");

        Assert.That(result.Value, Is.EqualTo("hello"));
    }

    [Test]
    public async Task Binary_decoder_deserializes_utf8_json_payload()
    {
        var decoder = new JsonBinaryPayloadDecoder<TestPayload>();
        var payload = Encoding.UTF8.GetBytes("{\"Value\":\"hello\"}");

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
    public void Decoders_honor_pre_cancelled_tokens()
    {
        var stringDecoder = new JsonStringPayloadDecoder<TestPayload>();
        var binaryDecoder = new JsonBinaryPayloadDecoder<TestPayload>();
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(
            async () => await stringDecoder.DecodeAsync("{}", cancellationTokenSource.Token));
        Assert.ThrowsAsync<OperationCanceledException>(
            async () => await binaryDecoder.DecodeAsync("{}"u8.ToArray(), cancellationTokenSource.Token));
    }

    private sealed record TestPayload(string Value);
}