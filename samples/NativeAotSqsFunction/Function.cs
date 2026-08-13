using Kralizek.Lambda;

using Microsoft.Extensions.DependencyInjection;

namespace NativeAotSqsFunction;

public sealed class Function : SqsFunction<OrderCreated, OrderCreatedHandler>
{
    protected override void ConfigureFrameworkServices(IServiceCollection services) =>
        services.AddSingleton(PayloadJsonSerializerContext.Default.OrderCreated);
}
