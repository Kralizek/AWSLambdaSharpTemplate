using Amazon.Lambda.Core;

using Kralizek.Lambda;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace SqsFunction;

public sealed class Function : SqsFunction<OrderCreated, OrderCreatedHandler>;