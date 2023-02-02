using Amazon.Lambda.SNSEvents;

namespace Kralizek.Lambda
{
    /// <summary>
    /// A base class that can be used for SNS Event Functions.
    /// </summary>
    public abstract class SnsEventFunction : EventFunction<SNSEvent>
    {
    }
}
