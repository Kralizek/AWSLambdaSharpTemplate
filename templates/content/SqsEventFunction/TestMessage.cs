using Newtonsoft.Json;

namespace SqsEventFunction
{
    /*
        This class represents the message you push into SQS and that is forwarded to this Lambda
        Add properties so that the message can be properly deserialized.
    */
    public class TestMessage
    {
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}