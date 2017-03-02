using Microsoft.Extensions.DependencyInjection;

namespace Template
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRequestResponseHandler<THandler, TInput, TOutput>(this IServiceCollection services)
            where THandler : class, IRequestResponseHandler<TInput, TOutput>
        {
            services.AddTransient<IRequestResponseHandler<TInput, TOutput>, THandler>();
            return services;
        } 
    }
}