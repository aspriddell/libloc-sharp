// libloc-sharp - A version of IPFire's libloc library rewritten for .NET
// Licensed under LGPL-2.1 - see the license file for more information

using System;
using DragonFruit.Data;
using DragonFruit.Data.Serializers;
using libloc.Access;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace libloc.Tests
{
    internal static class TestServices
    {
        public static IServiceProvider Services { get; } = GetServices();

        private static IServiceProvider GetServices()
        {
            var builder = Host.CreateApplicationBuilder();

            builder.Services.AddSingleton<ApiClient>(new ApiClient<ApiJsonSerializer>
            {
                UserAgent = "libloc-sharp-test"
            });

            builder.Services.AddLocationDb();
            return builder.Build().Services;
        }
    }
}