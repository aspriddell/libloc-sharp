// libloc-sharp - A version of IPFire's libloc library rewritten for .NET
// Licensed under LGPL-2.1 - see the license file for more information

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using libloc.Abstractions;

namespace libloc.V1
{
    internal class DatabaseV1FlattenedNetworkTreeEnumerator : IEnumerator<IAddressLocatedNetwork>
    {
        private readonly Stack<IAddressLocatedNetwork> _pendingNetworkStack = new();
        private readonly IEnumerator<IAddressLocatedNetwork> _treeEnumerator;

        public DatabaseV1FlattenedNetworkTreeEnumerator(IEnumerator<IAddressLocatedNetwork> treeEnumerator)
        {
            _treeEnumerator = treeEnumerator;
        }

        public bool MoveNext()
        {
            while (TryTakeNext(out var nextNetwork))
            {
                var treeSubnets = new List<IAddressLocatedNetwork>();
                while (TryTakeNext(out var candidate))
                {
                    if (!nextNetwork.Network.Contains(candidate.Network))
                    {
                        _pendingNetworkStack.Push(candidate);
                        break;
                    }

                    treeSubnets.Add(candidate);
                }

                if (treeSubnets.Count == 0)
                {
                    Current = nextNetwork;
                    return true;
                }

                var gaps = new List<IPNetwork2> { nextNetwork.Network };
                foreach (var subnet in IPNetwork2.Supernet(treeSubnets.Select(network => network.Network).ToArray()))
                {
                    gaps = gaps.SelectMany(network => network.Subtract(subnet)).ToList();
                }

                treeSubnets.AddRange(gaps.Select(network => new DatabaseNetwork(network, nextNetwork.CountryCode, nextNetwork.ASN, nextNetwork.Flags)));
                treeSubnets.Sort((left, right) =>
                {
                    var comparison = left.Network.Network.GetAddressBytes().AsSpan().SequenceCompareTo(right.Network.Network.GetAddressBytes());
                    return comparison != 0 ? comparison : left.Network.Cidr.CompareTo(right.Network.Cidr);
                });

                for (var index = treeSubnets.Count - 1; index >= 0; index--)
                {
                    _pendingNetworkStack.Push(treeSubnets[index]);
                }
            }

            Current = null;
            return false;
        }

        private bool TryTakeNext(out IAddressLocatedNetwork network)
        {
            if (_pendingNetworkStack.TryPop(out network))
            {
                return true;
            }

            if (_treeEnumerator.MoveNext())
            {
                network = _treeEnumerator.Current;
                return true;
            }

            network = null;
            return false;
        }

        public void Reset()
        {
            _treeEnumerator.Reset();
            _pendingNetworkStack.Clear();

            Current = null;
        }

        public IAddressLocatedNetwork Current { get; private set; }
        object IEnumerator.Current => Current;

        public void Dispose()
        {
            _treeEnumerator.Dispose();
        }
    }
}