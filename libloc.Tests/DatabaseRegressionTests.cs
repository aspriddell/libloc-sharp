using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using libloc.Abstractions;
using libloc.Exceptions;
using NUnit.Framework;

namespace libloc.Tests
{
    public class DatabaseRegressionTests
    {
        private const int TreeOffset = 16384;
        private const int PoolOffset = 24579;

        [TestCase(1u)]
        [TestCase(0x80000000u)]
        [TestCase(uint.MaxValue)]
        public void InvalidStringOffsetIsRejected(uint offset)
        {
            var bytes = CreateFixture(1);
            Write(bytes, 16, offset);
            using var database = LoadFixture(bytes);

            Assert.Throws<FileFormatException>(() => _ = database.Vendor);
        }

        [Test]
        public void UnterminatedStringPoolIsRejected()
        {
            var bytes = CreateFixture(1);
            bytes[PoolOffset] = 65;

            Assert.Throws<FileFormatException>(() => LoadFixture(bytes).Dispose());
        }

        [Test]
        public void EmptyStringPoolIsRejected()
        {
            var bytes = CreateFixture(1);
            Write(bytes, 64, 0);

            Assert.Throws<FileFormatException>(() => LoadFixture(bytes).Dispose());
        }

        [Test]
        public void UnalignedStringPoolSupportsUtf8AndFinalEmptyString()
        {
            var bytes = CreateFixture(1);
            var text = Encoding.UTF8.GetBytes("M\u00fcnchen\0");
            text.CopyTo(bytes, PoolOffset);
            Write(bytes, 64, (uint)text.Length);
            Write(bytes, 20, (uint)text.Length - 1);
            using var database = LoadFixture(bytes);

            Assert.That(database.Vendor, Is.EqualTo("M\u00fcnchen"));
            Assert.That(database.Description, Is.Empty);
        }

        private static IEnumerable<TestCaseData> PrefixLengthCases()
        {
            foreach (var address in new[] { "::", "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff", "2001:db8:1234:5678:9abc:def0:1234:5678", "192.0.2.1" })
            {
                for (var prefix = 0; prefix <= 128; prefix++)
                {
                    yield return new TestCaseData(address, prefix);
                }
            }
        }

        [TestCaseSource(nameof(PrefixLengthCases))]
        public void LookupSupportsEveryPrefixLength(string address, int prefix)
        {
            var ipAddress = IPAddress.Parse(address);
            var addressBytes = ipAddress.MapToIPv6().GetAddressBytes();
            var bytes = CreateFixture(prefix + 1);
            for (var depth = 0; depth < prefix; depth++)
            {
                var bit = (addressBytes[depth / 8] >> (7 - depth % 8)) & 1;
                Write(bytes, TreeOffset + depth * 12 + bit * 4, (uint)(depth + 1));
            }

            Write(bytes, TreeOffset + prefix * 12 + 8, 1);
            using var database = LoadFixture(bytes);
            var result = database.ResolveAddress(ipAddress);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ASN, Is.EqualTo(200));
            Assert.That(result.Network.ToString(), Is.EqualTo(IPNetwork2.Parse(ipAddress.MapToIPv6() + "/" + prefix).ToString()));
        }

        [TestCase(false, 100, "::/0")]
        [TestCase(true, 200, "::/1")]
        public void LookupReturnsMostSpecificMatchingAncestor(bool childHasNetwork, int expectedAsn, string expectedNetwork)
        {
            var bytes = CreateFixture(2);
            Write(bytes, TreeOffset, 1);
            Write(bytes, TreeOffset + 8, 0);
            if (childHasNetwork)
            {
                Write(bytes, TreeOffset + 20, 1);
            }

            using var database = LoadFixture(bytes);
            var result = database.ResolveAddress(IPAddress.IPv6Any);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ASN, Is.EqualTo(expectedAsn));
            Assert.That(result.Network.ToString(), Is.EqualTo(expectedNetwork));
        }

        [Test]
        public void LookupWithoutMatchingAncestorReturnsNull()
        {
            using var database = LoadFixture(CreateFixture(1));

            Assert.That(database.ResolveAddress(IPAddress.IPv6Any), Is.Null);
        }

        [Test]
        public void DisposeClosesEveryMappedCollection()
        {
            var database = LoadFixture(CreateFixture(1));
            database.Dispose();

            Assert.Throws<ObjectDisposedException>(() => _ = database.Vendor);
            Assert.Throws<ObjectDisposedException>(() => _ = database.Countries[0]);
            Assert.Throws<ObjectDisposedException>(() => _ = database.AS[0]);
            Assert.Throws<ObjectDisposedException>(() => _ = database.Networks[0]);
            using var enumerator = database.GetEnumerator();
            Assert.Throws<ObjectDisposedException>(() => enumerator.MoveNext());
            Assert.DoesNotThrow(database.Dispose);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FlatteningPreservesNestedMetadataAndParentGaps(bool nested)
        {
            var bytes = CreateFixture(nested ? 3 : 2);
            Write(bytes, TreeOffset, 1);
            Write(bytes, TreeOffset + 8, 0);
            Write(bytes, TreeOffset + 20, 1);
            if (nested)
            {
                Write(bytes, TreeOffset + 12, 2);
                Write(bytes, TreeOffset + 32, 2);
            }

            using var database = LoadFixture(bytes);
            using var enumerator = database.GetEnumerator(AddressFamily.InterNetworkV6, true);
            var expected = nested
                ? new[] { "::/2:300:DE:4", "4000::/2:200:US:2", "8000::/1:100:GB:1" }
                : new[] { "::/1:200:US:2", "8000::/1:100:GB:1" };

            Assert.That(ReadNetworks(enumerator), Is.EqualTo(expected));
            enumerator.Reset();
            Assert.That(ReadNetworks(enumerator), Is.EqualTo(expected));
        }

        [Test]
        public void FlatteningOmitsFullyCoveredParent()
        {
            var bytes = CreateFixture(3);
            Write(bytes, TreeOffset, 1);
            Write(bytes, TreeOffset + 4, 2);
            Write(bytes, TreeOffset + 8, 0);
            Write(bytes, TreeOffset + 20, 1);
            Write(bytes, TreeOffset + 32, 2);
            using var database = LoadFixture(bytes);
            using var enumerator = database.GetEnumerator(AddressFamily.InterNetworkV6, true);

            Assert.That(ReadNetworks(enumerator), Is.EqualTo(new[] { "::/1:200:US:2", "8000::/1:300:DE:4" }));
        }

        [TestCase("::/0", "::/1", "::/2", AddressFamily.InterNetworkV6)]
        [TestCase("2001:db8::/32", "2001:db8:8000::/33", "2001:db8:9000::/37", AddressFamily.InterNetworkV6)]
        [TestCase("::ffff:192.0.2.0/120", "::ffff:192.0.2.64/122", "::ffff:192.0.2.96/123", AddressFamily.InterNetwork)]
        [TestCase("2001:db8::/126", "2001:db8::/127", "2001:db8::1/128", AddressFamily.InterNetworkV6)]
        public void FlatteningPreservesCoverageAndBoundaryMetadata(string parent, string child, string grandchild, AddressFamily family)
        {
            using var database = LoadFixture(CreateTreeFixture((parent, 0), (child, 1), (grandchild, 2)));
            using var enumerator = database.GetEnumerator(family, true);
            var networks = new List<IAddressLocatedNetwork>();
            while (enumerator.MoveNext())
            {
                networks.Add(enumerator.Current);
            }

            Assert.That(networks, Is.Not.Empty);
            var parentNetwork = IPNetwork2.Parse(parent);
            var covered = BigInteger.Zero;
            for (var index = 0; index < networks.Count; index++)
            {
                var network = networks[index];
                Assert.That(parentNetwork.Contains(network.Network), Is.True);
                var size = BigInteger.One << (128 - network.Network.Cidr);
                covered += size;

                for (var other = index + 1; other < networks.Count; other++)
                {
                    Assert.That(network.Network.Contains(networks[other].Network.Network)
                        || networks[other].Network.Contains(network.Network.Network), Is.False,
                        $"Overlapping flattened ranges: {network.Network} and {networks[other].Network}");
                }

                var first = new BigInteger(network.Network.Network.GetAddressBytes(), isUnsigned: true, isBigEndian: true);
                foreach (var boundary in new[] { first, first + size - 1 })
                {
                    var boundaryBytes = boundary.ToByteArray(isUnsigned: true, isBigEndian: true);
                    var addressBytes = new byte[16];
                    boundaryBytes.CopyTo(addressBytes, addressBytes.Length - boundaryBytes.Length);
                    var address = new IPAddress(addressBytes);
                    var resolved = database.ResolveAddress(address);
                    Assert.That(resolved, Is.Not.Null, $"No lookup result at {address}");
                    Assert.That((resolved.ASN, resolved.CountryCode, resolved.Flags),
                        Is.EqualTo((network.ASN, network.CountryCode, network.Flags)),
                        $"Metadata differs at {address} in {network.Network}");
                }
            }

            Assert.That(covered, Is.EqualTo(BigInteger.One << (128 - parentNetwork.Cidr)));
        }

        [TestCase(AddressFamily.Unspecified, false)]
        [TestCase(AddressFamily.InterNetwork, false)]
        [TestCase(AddressFamily.InterNetwork, true)]
        [TestCase(AddressFamily.InterNetworkV6, false)]
        [TestCase(AddressFamily.InterNetworkV6, true)]
        public void EnumerationFiltersMixedAddressFamilies(AddressFamily family, bool flattened)
        {
            using var database = LoadFixture(CreateTreeFixture(("192.0.2.0/24", 1), ("2001:db8::/32", 2)));
            using var enumerator = family == AddressFamily.Unspecified
                ? database.GetEnumerator()
                : database.GetEnumerator(family, flattened);
            var expected = new List<string>();
            if (family != AddressFamily.InterNetworkV6)
            {
                expected.Add("::ffff:192.0.2.0/120:200:US:2");
            }

            if (family != AddressFamily.InterNetwork)
            {
                expected.Add("2001:db8::/32:300:DE:4");
            }

            Assert.That(ReadNetworks(enumerator), Is.EqualTo(expected));
            Assert.That(enumerator.MoveNext(), Is.False);
        }

        [TestCase(AddressFamily.InterNetwork, false, false)]
        [TestCase(AddressFamily.InterNetwork, false, true)]
        [TestCase(AddressFamily.InterNetwork, true, false)]
        [TestCase(AddressFamily.InterNetwork, true, true)]
        [TestCase(AddressFamily.InterNetworkV6, false, false)]
        [TestCase(AddressFamily.InterNetworkV6, false, true)]
        [TestCase(AddressFamily.InterNetworkV6, true, false)]
        [TestCase(AddressFamily.InterNetworkV6, true, true)]
        public void EnumerationResetRestoresAddressFamilyState(AddressFamily family, bool flattened, bool exhaust)
        {
            using var database = LoadFixture(CreateTreeFixture(("::/0", 0), ("192.0.2.0/24", 1)));
            using var enumerator = database.GetEnumerator(family, flattened);
            var expected = family == AddressFamily.InterNetwork
                ? new[] { "::ffff:192.0.2.0/120:200:US:2" }
                : new[] { "::/0:100:GB:1" };

            Assert.That(enumerator.MoveNext(), Is.True);
            Assert.That(enumerator.Current.ASN, Is.EqualTo(family == AddressFamily.InterNetwork ? 200 : 100));
            if (exhaust)
            {
                Assert.That(ReadNetworks(enumerator), Is.Empty);
            }

            enumerator.Reset();
            Assert.That(ReadNetworks(enumerator), Is.EqualTo(expected));
            enumerator.Reset();
            Assert.That(ReadNetworks(enumerator), Is.EqualTo(expected));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void UnfilteredEnumerationResetRestartsMixedTree(bool exhaust)
        {
            using var database = LoadFixture(CreateTreeFixture(("192.0.2.0/24", 1), ("2001:db8::/32", 2)));
            var expected = database.Select(network => network.Network.ToString()).ToArray();
            using var enumerator = database.GetEnumerator();
            Assert.That(enumerator.MoveNext(), Is.True);
            if (exhaust)
            {
                while (enumerator.MoveNext())
                {
                }
            }

            enumerator.Reset();
            var actual = new List<string>();
            while (enumerator.MoveNext())
            {
                actual.Add(enumerator.Current.Network.ToString());
            }

            Assert.That(actual, Is.EqualTo(expected));
        }

        private static byte[] CreateTreeFixture(params (string Network, uint Record)[] networks)
        {
            var nodes = new List<(uint Zero, uint One, uint Network)> { (0, 0, uint.MaxValue) };
            foreach (var entry in networks)
            {
                var network = IPNetwork2.Parse(entry.Network);
                var address = network.Network.MapToIPv6().GetAddressBytes();
                var prefix = network.Cidr + (network.Network.AddressFamily == AddressFamily.InterNetwork ? 96 : 0);
                var index = 0;
                for (var depth = 0; depth < prefix; depth++)
                {
                    var bit = (address[depth / 8] >> (7 - depth % 8)) & 1;
                    var node = nodes[index];
                    var next = bit == 0 ? node.Zero : node.One;
                    if (next == 0)
                    {
                        next = (uint)nodes.Count;
                        nodes.Add((0, 0, uint.MaxValue));
                        if (bit == 0)
                        {
                            node.Zero = next;
                        }
                        else
                        {
                            node.One = next;
                        }

                        nodes[index] = node;
                    }

                    index = (int)next;
                }

                var leaf = nodes[index];
                leaf.Network = entry.Record;
                nodes[index] = leaf;
            }

            var bytes = CreateFixture(nodes.Count);
            for (var index = 0; index < nodes.Count; index++)
            {
                Write(bytes, TreeOffset + index * 12, nodes[index].Zero);
                Write(bytes, TreeOffset + index * 12 + 4, nodes[index].One);
                Write(bytes, TreeOffset + index * 12 + 8, nodes[index].Network);
            }

            return bytes;
        }

        private static string[] ReadNetworks(IEnumerator<IAddressLocatedNetwork> enumerator)
        {
            var networks = new List<string>();
            while (enumerator.MoveNext())
            {
                var network = enumerator.Current;
                networks.Add(network.Network + ":" + network.ASN + ":" + network.CountryCode + ":" + (int)network.Flags);
            }

            return networks.ToArray();
        }

        private static byte[] CreateFixture(int nodeCount)
        {
            var bytes = new byte[32768];
            Encoding.ASCII.GetBytes("LOCDBXX").CopyTo(bytes, 0);
            bytes[7] = 1;
            Write(bytes, 28, 8192);
            Write(bytes, 32, 8);
            Write(bytes, 36, 12288);
            Write(bytes, 40, 36);
            Write(bytes, 44, TreeOffset);
            Write(bytes, 48, (uint)(nodeCount * 12));
            Write(bytes, 52, 20480);
            Write(bytes, 56, 8);
            Write(bytes, 60, PoolOffset);
            Write(bytes, 64, 1);
            Encoding.ASCII.GetBytes("GB").CopyTo(bytes, 12288);
            Encoding.ASCII.GetBytes("US").CopyTo(bytes, 12300);
            Encoding.ASCII.GetBytes("DE").CopyTo(bytes, 12312);
            Write(bytes, 12292, 100);
            Write(bytes, 12304, 200);
            Write(bytes, 12316, 300);
            BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(12296, 2), 1);
            BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(12308, 2), 2);
            BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(12320, 2), 4);
            for (var index = 0; index < nodeCount; index++)
            {
                Write(bytes, TreeOffset + index * 12 + 8, uint.MaxValue);
            }

            return bytes;
        }

        private static void Write(byte[] bytes, int offset, uint value)
        {
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(offset, 4), value);
        }

        private static ILocationDatabase LoadFixture(byte[] bytes)
        {
            var stream = new FileStream(Path.GetTempFileName(), FileMode.Open, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.DeleteOnClose);
            try
            {
                stream.Write(bytes);
                stream.Position = 0;
                return DatabaseLoader.LoadFromStream(stream);
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }
    }
}