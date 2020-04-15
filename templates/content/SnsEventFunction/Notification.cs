using System.Text.Json;

namespace SnsEventFunction
{
    /*
        This class represents the notification you push into SNS and that is forwarded to this Lambda
        Add properties so that the message can be properly deserialized.
    */
    public class Notification
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}