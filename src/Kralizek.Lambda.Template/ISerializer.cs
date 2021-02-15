namespace Kralizek.Lambda
{
    public interface ISerializer
    {
        public T Deserialize<T>(string input);
    }
}