using Amazon.Lambda.CloudWatchEvents;

namespace Kralizek.Lambda;

/// <summary>
/// A function base class for Lambda functions triggered by Amazon EventBridge.
/// </summary>
/// <typeparam name="TDetail">The strongly typed EventBridge event detail.</typeparam>
/// <typeparam name="THandler">The concrete handler type that processes the event.</typeparam>
public abstract class EventBridgeFunction<TDetail, THandler>
    : EventFunction<CloudWatchEvent<TDetail>, THandler>
    where THandler : class, IEventBridgeHandler<TDetail>;
