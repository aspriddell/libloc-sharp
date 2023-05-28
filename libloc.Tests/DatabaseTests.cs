using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using libloc.Access;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

namespace libloc.Tests
{
    public class Tests
    {
        [OneTimeSetUp]
        public async Task Setup()
        {
            var locationDbService = (IHostedService)TestServices.Services.GetRequiredService<ILocationDbAccessor>();

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            await locationDbService.StartAsync(timeout.Token);
        }

        [OneTimeTearDown]
        public async Task Cleanup()
        {
            var locationDbService = (IHostedService)TestServices.Services.GetRequiredService<ILocationDbAccessor>();

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await locationDbService.StopAsync(timeout.Token);
        }

        [TestCase("2a00:23c7:acb:ef01::", "2a00:2380::", 2856)]
        public async Task TestAddressResolution(string address, string expectedStartBlock, int expectedAs)
        {
            if (!IPAddress.TryParse(address, out var targetAddress))
            {
                Assert.Inconclusive($"{nameof(address)} was not a valid address");
            }

            var database = TestServices.Services.GetRequiredService<ILocationDbAccessor>();
            var addressInfo = await database.PerformAsync(db => db.ResolveAddress(targetAddress)).ConfigureAwait(false);

            Assert.IsNotNull(addressInfo);
            Assert.That(addressInfo.ASN, Is.EqualTo(expectedAs));
            Assert.That(addressInfo.FirstAddress, Is.EqualTo(IPAddress.Parse(expectedStartBlock)));
        }
    }
}