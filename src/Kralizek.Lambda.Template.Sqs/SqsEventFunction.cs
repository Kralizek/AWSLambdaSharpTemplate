using Amazon.Lambda.SQSEvents;

namespace Kralizek.Lambda
{
    /// <summary>
    /// A base class that can be used for SQS Event Functions.
    /// </summary>
    public abstract class SqsEventFunction : EventFunction<SQSEvent>
    {
    }
}
