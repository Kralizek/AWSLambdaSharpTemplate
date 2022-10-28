using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Kralizek.Lambda;

namespace Tests.Lambda.Sns;

public class TestNotification
{
        
}

public class TestNotificationHandler : INotificationHandler<TestNotification>
{
    public  Task HandleAsync(TestNotification notification, ILambdaContext context) => Task.CompletedTask;
}