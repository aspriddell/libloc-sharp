// libloc-sharp - A version of IPFire's libloc library rewritten for .NET
// Licensed under LGPL-2.1 - see the license file for more information

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Net;
using System.Net.Sockets;
using libloc.Abstractions;
using libloc.V1.Collections;
using libloc.V1.Source;

namespace libloc.V1
{
    public class DatabaseV1 : ILocationDatabase
    {
        private readonly DatabaseV1AS _as;
        private readonly DatabaseV1Countries _countries;
        private readonly MemoryMappedFile _mmdb;
        private readonly DatabaseV1Networks _networks;

        private readonly DatabaseV1NetworkTree _networkTree;
        private readonly DatabaseV1StringPool _stringPool;

        private readonly uint _vendorStringLoc, _descriptionStringLoc, _licenseStringLoc;

        internal unsafe DatabaseV1(MemoryMappedFile mmdb)
        {
            _mmdb = mmdb;
            DatabaseV1Header header;

            using (var headerView = mmdb.CreateViewAccessor(sizeof(GlobalDatabaseHeader), sizeof(DatabaseV1Header), MemoryMappedFileAccess.Read))
            {
                headerView.Read(0, out header);
            }

            // copy header info over (endianess is corrected at the stringpool)
            _vendorStringLoc = header.vendor;
            _licenseStringLoc = header.license;
            _descriptionStringLoc = header.description;

            CreatedAt = DateTimeOffset.FromUnixTimeSeconds((long)BinaryUtils.EnsureEndianness(header.created_at));

            // create object views
            var networkTreeView = mmdb.CreateViewAccessor(BinaryUtils.EnsureEndianness(header.network_tree_offset), BinaryUtils.EnsureEndianness(header.network_tree_length), MemoryMappedFileAccess.Read);
            _networkTree = new DatabaseV1NetworkTree(networkTreeView);

            var stringPoolView = mmdb.CreateViewAccessor(BinaryUtils.EnsureEndianness(header.pool_offset), BinaryUtils.EnsureEndianness(header.pool_length), MemoryMappedFileAccess.Read);
            _stringPool = new DatabaseV1StringPool(stringPoolView);

            var asView = mmdb.CreateViewAccessor(BinaryUtils.EnsureEndianness(header.as_offset), BinaryUtils.EnsureEndianness(header.as_length), MemoryMappedFileAccess.Read);
            _as = new DatabaseV1AS(asView, _stringPool);

            var networksView = mmdb.CreateViewAccessor(BinaryUtils.EnsureEndianness(header.network_data_offset), BinaryUtils.EnsureEndianness(header.network_data_length), MemoryMappedFileAccess.Read);
            _networks = new DatabaseV1Networks(networksView);

            var countryView = mmdb.CreateViewAccessor(BinaryUtils.EnsureEndianness(header.countries_offset), BinaryUtils.EnsureEndianness(header.countries_length), MemoryMappedFileAccess.Read);
            _countries = new DatabaseV1Countries(countryView, _stringPool);
        }

        public int Version => 1;

        public string Vendor => _stringPool[_vendorStringLoc];
        public string License => _stringPool[_licenseStringLoc];
        public string Description => _stringPool[_descriptionStringLoc];

        public DateTimeOffset CreatedAt { get; }

        public IASDatabase AS => _as;
        public INetworkDatabase Networks => _networks;
        public ICountryDatabase Countries => _countries;

        public IAddressLocatedNetwork ResolveAddress(IPAddress address)
        {
            var depth = -1;
            int nextNodeIndex = 0;

            var mappedAddress = address.MapToIPv6().GetAddressBytes();
            Span<byte> networkAddress = stackalloc byte[mappedAddress.Length];

            DatabaseSourceNetworkNode node;

            do
            {
                if (nextNodeIndex >= _networkTree.Count)
                {
                    return null;
                }

                // get the bit to perform next node indexing on
                var bit = AddressUtils.GetAddressBit(mappedAddress, ++depth);

                AddressUtils.SetAddressBit(networkAddress, depth, bit);

                node = _networkTree.ElementAt(nextNodeIndex);
                nextNodeIndex = (int)BinaryUtils.EnsureEndianness(bit == 0 ? node.zero : node.one);
            } while (nextNodeIndex > 0);

            if (!node.IsLeaf)
            {
                return null;
            }

            var networkIndex = BinaryUtils.EnsureEndianness(node.network);
            return _networks.CreateWithPrefix(networkIndex, networkAddress, depth);
        }

        public void Dispose()
        {
            _stringPool.Dispose();
            _countries.Dispose();

            _mmdb.Dispose();
        }

        public IEnumerator<IAddressLocatedNetwork> GetEnumerator(AddressFamily addressFamily, bool flattened)
        {
            if (addressFamily is not AddressFamily.InterNetwork and not AddressFamily.InterNetworkV6)
            {
                throw new ArgumentException($"Only {nameof(AddressFamily.InterNetwork)} or {nameof(AddressFamily.InterNetworkV6)} can be used as a filter");
            }

            var treeEnumerator = new DatabaseV1NetworkTreeEnumerator(_networkTree, _networks, addressFamily);
            return flattened ? new DatabaseV1FlattenedNetworkTreeEnumerator(treeEnumerator) : treeEnumerator;
        }

        public IEnumerator<IAddressLocatedNetwork> GetEnumerator()
        {
            return new DatabaseV1NetworkTreeEnumerator(_networkTree, _networks, null);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}