using System.Threading.Tasks;
using Amazon.Lambda.Core;

namespace Kralizek.Lambda;

public interface IMessageHandler<in TMessage> where TMessage : class
{
    Task HandleAsync(TMessage? message, ILambdaContext context);
}