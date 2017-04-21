using System;
using Microsoft.Extensions.DependencyInjection;

namespace Kralizek.Lambda
{
    public static class UtilExtensions
    {
        public static IServiceScope CreateScope(this IServiceProvider serviceProvider)
        {
            return serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        }
    }
}