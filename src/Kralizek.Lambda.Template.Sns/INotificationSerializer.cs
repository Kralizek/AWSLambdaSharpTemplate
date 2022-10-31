using System.Text.Json;

namespace Kralizek.Lambda;

/// <summary>
/// An interface to describe a serializer of SNS notifications.
/// </summary>
public interface INotificationSerializer
{
    /// <summary>
    /// Deserializes a <see cref="string"/> into <typeparamref name="TMessage"/>.
    /// </summary>
    /// <param name="input">The input string to be deserialized.</param>
    /// <typeparam name="TMessage">The type of the result.</typeparam>
    /// <returns>The result. Can be null.</returns>
    public TMessage? Deserialize<TMessage>(string input);
}

/// <summary>
/// The default implementation of <see cref="INotificationSerializer"/> using <see cref="JsonSerializer"/>.
/// </summary>
public class DefaultJsonNotificationSerializer : INotificationSerializer
{
    /// <summary>
    /// Deserializes a <see cref="string"/> into <typeparamref name="TMessage"/> using <see cref="JsonSerializer"/>.
    /// </summary>
    /// <inheritdoc />
    public TMessage? Deserialize<TMessage>(string input) => JsonSerializer.Deserialize<TMessage>(input);
}