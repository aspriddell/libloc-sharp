// liblocsharp - A version of IPFire's libloc library rewritten for C#
// Licensed under LGPL-2.1 - see the license file for more information

using System;
using System.Net;
using System.Net.Http;
using DragonFruit.Data;
using DragonFruit.Data.Serializers.SystemJson;
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

            builder.Services.AddSingleton<ApiClient>(new ApiClient<ApiSystemTextJsonSerializer>
            {
                UserAgent = "libloc-sharp-test",
                Handler = () => new SocketsHttpHandler
                {
                    AutomaticDecompression = DecompressionMethods.None
                }
            });

            builder.Services.AddLocationDb();
            return builder.Build().Services;
        }
    }
}