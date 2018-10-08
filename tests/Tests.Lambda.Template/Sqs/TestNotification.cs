using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Kralizek.Lambda;

namespace Tests.Lambda.Sqs
{
    public class TestMessage
    {
        
    }

    public class TestMessageHandler : IMessageHandler<TestMessage>
    {
        public  Task HandleAsync(TestMessage message, ILambdaContext context) => Task.CompletedTask;
    }
}