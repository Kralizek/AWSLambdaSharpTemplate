using Kralizek.Lambda;

using Microsoft.Extensions.DependencyInjection;

namespace NativeAotSqsFunction;

public sealed class Function : SqsFunction<OrderCreated, OrderCreatedHandler>
{
    protected override void ConfigurePayloadServices(IServiceCollection services) =>
        services.AddSingleton<IStringPayloadDecoder<OrderCreated>>(
            new JsonStringPayloadDecoder<OrderCreated>(LambdaJsonSerializerContext.Default.OrderCreated));
}
