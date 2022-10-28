using System.Text.Json;

namespace Kralizek.Lambda;

public interface IMessageSerializer
{
    public TMessage Deserialize<TMessage>(string input);
}

public class DefaultJsonMessageSerializer : IMessageSerializer
{
    public TMessage Deserialize<TMessage>(string input) => JsonSerializer.Deserialize<TMessage>(input);
}