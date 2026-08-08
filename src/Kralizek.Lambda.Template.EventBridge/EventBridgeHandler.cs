using Amazon.Lambda.CloudWatchEvents;

namespace Kralizek.Lambda;

/// <summary>
/// Handles a strongly typed Amazon EventBridge event.
/// </summary>
/// <typeparam name="TDetail">The type of the EventBridge event detail.</typeparam>
public interface IEventBridgeHandler<TDetail>
    : IEventHandler<CloudWatchEvent<TDetail>, EventContext>;