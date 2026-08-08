using Amazon.Lambda.Core;

using Kralizek.Lambda;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EventBridgeFunction;

public sealed class Function : EventBridgeFunction<OrderCreated, OrderCreatedHandler>;