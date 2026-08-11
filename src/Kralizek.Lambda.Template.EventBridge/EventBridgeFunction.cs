using System.Diagnostics;

using Amazon.Lambda.CloudWatchEvents;
using Amazon.Lambda.Core;

namespace Kralizek.Lambda;

/// <summary>
/// A function base class for Lambda functions triggered by Amazon EventBridge.
/// </summary>
/// <typeparam name="TDetail">The strongly typed EventBridge event detail.</typeparam>
/// <typeparam name="THandler">The concrete handler type that processes the event.</typeparam>
public abstract class EventBridgeFunction<TDetail, THandler>
    : EventFunction<CloudWatchEvent<TDetail>, THandler>
    where THandler : class, IEventBridgeHandler<TDetail>
{
    protected override void EnrichInvocationActivity(
        Activity activity,
        CloudWatchEvent<TDetail> input,
        ILambdaContext context)
    {
        activity.SetTag("kralizek.aws.eventbridge.event_id", input.Id);
        activity.SetTag("kralizek.aws.eventbridge.source", input.Source);
        activity.SetTag("kralizek.aws.eventbridge.detail_type", input.DetailType);
    }
}
