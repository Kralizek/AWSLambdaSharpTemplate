using Kralizek.Lambda;

using Microsoft.Extensions.DependencyInjection;

namespace NativeAotSqsFunction;

public sealed class Function : SqsFunction<OrderCreated, OrderCreatedHandler>
{
    protected override void ConfigureFrameworkServices(IServiceCollection services)
    {
        services.AddScoped<OrderCreatedHandler>();
        services.AddSingleton<IStringPayloadDecoder<OrderCreated>>(
            new JsonStringPayloadDecoder<OrderCreated>(LambdaJsonSerializerContext.Default.OrderCreated));
    }
}
