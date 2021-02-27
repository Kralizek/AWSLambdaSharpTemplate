namespace Kralizek.Lambda
{
    public interface ISerializer
    {
        public T Deserialize<T>(string input);
    }

    public class SystemTextJsonSerializer : ISerializer
    {
        public T Deserialize<T>(string input) => System.Text.Json.JsonSerializer.Deserialize<T>(input);
    }
}