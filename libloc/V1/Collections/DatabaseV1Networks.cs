// libloc-sharp - A version of IPFire's libloc library rewritten for .NET
// Licensed under LGPL-2.1 - see the license file for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Net;
using System.Text;
using libloc.Abstractions;
using libloc.V1.Source;

namespace libloc.V1.Collections
{
    internal class DatabaseV1Networks : DatabaseV1Collection<DatabaseSourceNetwork>, INetworkDatabase
    {
        public DatabaseV1Networks(MemoryMappedViewAccessor view)
            : base(view)
        {
        }

        public IDatabaseNetwork this[int index] => FromSource(ElementAt(index), null);

        public IEnumerator<IDatabaseNetwork> GetEnumerator()
        {
            for (var i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        internal DatabaseNetwork CreateWithPrefix(uint index, Span<byte> v6AddressBytes, int prefix)
        {
            Debug.Assert(v6AddressBytes.Length == 16);

            var networkInfo = ElementAt((int)index);

            Span<byte> bitmask = stackalloc byte[v6AddressBytes.Length];
            Span<byte> firstAddressBytes = stackalloc byte[v6AddressBytes.Length];

            for (int i = prefix, j = 0; i > 0; i -= 8, j++)
            {
                bitmask[j] = i >= 8 ? (byte)0xff : (byte)(0xff << (8 - i));
            }

            for (var i = 0; i < v6AddressBytes.Length; i++)
            {
                // perform AND to get first address
                firstAddressBytes[i] = (byte)(v6AddressBytes[i] & bitmask[i]);
            }

            return FromSource(networkInfo, IPNetwork2.Parse(new IPAddress(firstAddressBytes), new IPAddress(bitmask)));
        }

        private unsafe DatabaseNetwork FromSource(DatabaseSourceNetwork source, IPNetwork2 network)
        {
            var correctedAsn = BinaryUtils.EnsureEndianness(source.asn);
            var correctedFlags = BinaryUtils.EnsureEndianness(source.flags);
            var countryCode = Encoding.ASCII.GetString(source.country_code, 2);

            return new DatabaseNetwork(network, countryCode, correctedAsn, (NetworkFlags)correctedFlags);
        }
    }
}