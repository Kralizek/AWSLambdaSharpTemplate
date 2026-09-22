using Amazon.Lambda;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kralizek.Lambda;

public static class TenantRoutingServiceCollectionExtensions
{
    /// <summary>
    /// Adds tenant-aware Lambda routing using the AWS Lambda SDK client.
    /// </summary>
    public static IServiceCollection AddTenantLambdaRouting(this IServiceCollection services)
    {
        services.TryAddSingleton<IAmazonLambda, AmazonLambdaClient>();
        services.TryAddSingleton<ITenantLambdaRouter>(serviceProvider =>
            new TenantLambdaRouter(serviceProvider.GetRequiredService<IAmazonLambda>()));

        return services;
    }
}
