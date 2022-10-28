using System.Text.Json;

namespace Kralizek.Lambda;

public interface INotificationSerializer
{
    public TMessage? Deserialize<TMessage>(string input);
}

public class DefaultJsonNotificationSerializer : INotificationSerializer
{
    public TMessage? Deserialize<TMessage>(string input) => JsonSerializer.Deserialize<TMessage>(input);
}