using Kralizek.Lambda;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NativeAotSqsFunction;

public sealed class Function : SqsFunction<OrderCreated, OrderCreatedHandler>
{
    protected override void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration)
    {
        base.ConfigureServices(services, configuration);

        services.AddSingleton<IStringPayloadDecoder<OrderCreated>>(
            new JsonStringPayloadDecoder<OrderCreated>(LambdaJsonSerializerContext.Default.OrderCreated));
    }
}
