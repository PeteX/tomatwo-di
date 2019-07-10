using System;
using Microsoft.Extensions.DependencyInjection;

namespace Tomatwo.DependencyInjection
{
    public static class EnhancedServiceProviderExtensions
    {
        /// <summary>
        /// Scans the registered services, and creates subclasses where necessary to implement property/field injection
        /// or interception.
        /// </summary>
        public static IServiceCollection AddEnhancedServiceProvider(this IServiceCollection services,
            Action<EnhancedServiceProvider> config = null)
        {
            new EnhancedServiceProvider(services, config);
            return services;
        }
    }
}
