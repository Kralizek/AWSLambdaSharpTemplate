using System.Threading.Tasks;
using Amazon.Lambda.Core;

namespace Kralizek.Lambda;

/// <summary>
/// An interface that describes an handler for SQS messages whose internal type is <typeparamref name="TMessage"/>.
/// </summary>
/// <typeparam name="TMessage">The internal type of the SQS message.</typeparam>
public interface IMessageHandler<in TMessage> where TMessage : class
{
    Task HandleAsync(TMessage? message, ILambdaContext context);
}