using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kralizek.Lambda;

/// <summary>
/// Decodes a text payload into an application contract.
/// </summary>
/// <typeparam name="TPayload">The application contract type produced by the decoder.</typeparam>
public interface IStringPayloadDecoder<TPayload>
{
    ValueTask<TPayload> DecodeAsync(string payload, CancellationToken cancellationToken = default);
}

/// <summary>
/// Decodes a binary payload into an application contract.
/// </summary>
/// <typeparam name="TPayload">The application contract type produced by the decoder.</typeparam>
public interface IBinaryPayloadDecoder<TPayload>
{
    ValueTask<TPayload> DecodeAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default);
}