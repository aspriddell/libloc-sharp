// libloc-sharp - A version of IPFire's libloc library rewritten for .NET
// Licensed under LGPL-2.1 - see the license file for more information

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Net;
using System.Net.Sockets;
using libloc.Abstractions;
using libloc.Exceptions;
using libloc.V1.Collections;

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

            if (header.pool_length == 0)
            {
                throw new FileFormatException("The string pool is empty");
            }

            var views = new List<MemoryMappedViewAccessor>();
            try
            {
                _networkTree = new DatabaseV1NetworkTree(CreateView(header.network_tree_offset, header.network_tree_length));
                _stringPool = new DatabaseV1StringPool(CreateView(header.pool_offset, header.pool_length));
                _as = new DatabaseV1AS(CreateView(header.as_offset, header.as_length), _stringPool);
                _networks = new DatabaseV1Networks(CreateView(header.network_data_offset, header.network_data_length));
                _countries = new DatabaseV1Countries(CreateView(header.countries_offset, header.countries_length), _stringPool);
            }
            catch
            {
                foreach (var view in views)
                {
                    view.Dispose();
                }

                throw;
            }

            MemoryMappedViewAccessor CreateView(uint offset, uint length)
            {
                var view = mmdb.CreateViewAccessor(BinaryUtils.EnsureEndianness(offset), BinaryUtils.EnsureEndianness(length), MemoryMappedFileAccess.Read);
                views.Add(view);
                return view;
            }
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
            _as.Dispose();
            _networks.Dispose();
            _networkTree.Dispose();

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