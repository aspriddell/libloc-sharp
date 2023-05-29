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

        [TestCase("140.238.99.135", "140.238.96.0", 31898)]
        [TestCase("2001:67c:2e8:22::c100:691", "2001:67c:2e8::", 3333)]
        [TestCase("2a00:23c7:acb:ef01::", "2a00:2380::", 2856)]
        public async Task TestAddressResolution(string address, string expectedStartBlock, int expectedAs)
        {
            var database = TestServices.Services.GetRequiredService<ILocationDbAccessor>();
            var addressInfo = await database.PerformAsync(db => db.ResolveAddress(IPAddress.Parse(address))).ConfigureAwait(false);

            Assert.IsNotNull(addressInfo);
            Assert.That(addressInfo.ASN, Is.EqualTo(expectedAs));
            Assert.That(addressInfo.FirstAddress, Is.EqualTo(IPAddress.Parse(expectedStartBlock).MapToIPv6()));
        }

        [TestCase("10.11.12.13")]
        [TestCase("127.0.20.100")]
        [TestCase("fc00::2001:abcd:0010")]
        public async Task TestUnassignedAddressResolution(string address)
        {
            if (!IPAddress.TryParse(address, out var ipAddress))
            {
                Assert.Inconclusive();
                return;
            }

            var database = TestServices.Services.GetRequiredService<ILocationDbAccessor>();
            var addressInfo = await database.PerformAsync(db => db.ResolveAddress(ipAddress));

            Assert.IsNull(addressInfo);
        }

        [TestCase(786U)]
        [TestCase(3333U)]
        public async Task TestASLookup(uint asn)
        {
            var database = TestServices.Services.GetRequiredService<ILocationDbAccessor>();
            var asInfo = await database.PerformAsync(db => db.AS[asn]);

            Assert.IsNotNull(asInfo);
            Assert.IsNotEmpty(asInfo.Name);
        }
    }
}