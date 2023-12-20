// libloc-sharp - A version of IPFire's libloc library rewritten for .NET
// Licensed under LGPL-2.1 - see the license file for more information

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
            IAddressLocatedNetwork nextNetwork;

            if (!_pendingNetworkStack.TryPop(out nextNetwork))
            {
                // move next in base tree
                if (!_treeEnumerator.MoveNext())
                {
                    return false;
                }

                nextNetwork = _treeEnumerator.Current!;
            }

            // walk the tree to create a list of subnets
            var treeSubnets = new List<IPNetwork>();

            while (_treeEnumerator.MoveNext())
            {
                if (nextNetwork.Network.Contains(_treeEnumerator.Current!.Network))
                {
                    treeSubnets.Add(_treeEnumerator.Current.Network);
                }
                else
                {
                    _pendingNetworkStack.Push(_treeEnumerator.Current);
                    break;
                }
            }

            // can stop here if there are no subnets associated with this network
            if (!treeSubnets.Any())
            {
                Current = nextNetwork;
                return true;
            }

            // combine all networks and get the gaps created as well...
            var nets = IPNetwork.Supernet(treeSubnets.ToArray());
            foreach (var network in nets)
            {
                _pendingNetworkStack.Push(new DatabaseNetwork(network, nextNetwork.CountryCode, nextNetwork.ASN, nextNetwork.Flags));
            }

            return MoveNext();
        }

        public void Reset()
        {
            _treeEnumerator.Reset();
            _pendingNetworkStack.Clear();
        }

        public IAddressLocatedNetwork Current { get; private set; }
        object IEnumerator.Current => Current;

        public void Dispose()
        {
            _treeEnumerator.Dispose();
        }
    }
}