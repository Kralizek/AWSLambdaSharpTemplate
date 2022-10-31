using System.Text.Json;

namespace Kralizek.Lambda;

/// <summary>
/// An interface to describe a serializer of SQS messages.
/// </summary>
public interface IMessageSerializer
{
    /// <summary>
    /// Deserializes a <see cref="string"/> into <typeparamref name="TMessage"/>.
    /// </summary>
    /// <param name="input">The input string to be deserialized.</param>
    /// <typeparam name="TMessage">The internal type of the SQS message.</typeparam>
    /// <returns>The result. Can be null.</returns>
    public TMessage? Deserialize<TMessage>(string input);
}

/// <summary>
/// The default implementation of <see cref="IMessageSerializer"/> using <see cref="JsonSerializer"/>.
/// </summary>
public class DefaultJsonMessageSerializer : IMessageSerializer
{
    /// <summary>
    /// Deserializes a <see cref="string"/> into <typeparamref name="TMessage"/> using <see cref="JsonSerializer"/>.
    /// </summary>
    /// <inheritdoc />
    public TMessage? Deserialize<TMessage>(string input) => JsonSerializer.Deserialize<TMessage>(input);
}