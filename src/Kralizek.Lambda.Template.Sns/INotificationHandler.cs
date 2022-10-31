using System.Threading.Tasks;
using Amazon.Lambda.Core;

namespace Kralizek.Lambda;

/// <summary>
/// An interface that describes an handler for SNS notifications whose internal type is <typeparamref name="TNotification"/>.
/// </summary>
/// <typeparam name="TNotification">The internal type of the SNS notification.</typeparam>
public interface INotificationHandler<in TNotification> where TNotification : class
{
    /// <summary>
    /// The method used to handle the notification.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <param name="context">A representation of the execution context.</param>
    Task HandleAsync(TNotification? notification, ILambdaContext context);
}