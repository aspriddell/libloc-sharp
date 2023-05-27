// liblocsharp - A version of IPFire's libloc library rewritten for C#
// Licensed under LGPL-2.1 - see the license file for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace libloc.Access
{
    public static class LocationDbExtensions
    {
        /// <summary>
        /// Adds the <see cref="ILocationDbAccessor"/> to the provided <see cref="IServiceCollection"/>
        /// and registers the database update system as a hosted service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        public static IServiceCollection AddLocationDb(this IServiceCollection services)
        {
            services.AddSingleton<ILocationDbAccessor, LocationDbAccessor>();
            services.AddHostedService(s => (IHostedService)s.GetRequiredService<ILocationDbAccessor>());

            return services;
        }
    }
}