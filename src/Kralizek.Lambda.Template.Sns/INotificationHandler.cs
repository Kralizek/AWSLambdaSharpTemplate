using System.Threading.Tasks;
using Amazon.Lambda.Core;

namespace Kralizek.Lambda;

public interface INotificationHandler<in TNotification> where TNotification : class
{
    Task HandleAsync(TNotification? notification, ILambdaContext context);
}