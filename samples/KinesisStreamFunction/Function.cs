using Amazon.Lambda.Core;

using Kralizek.Lambda;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace KinesisStreamFunction;

public sealed class Function : KinesisStreamFunction<OrderCreated, OrderCreatedHandler>;