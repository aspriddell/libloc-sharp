// libloc-sharp - A version of IPFire's libloc library rewritten for .NET
// Licensed under LGPL-2.1 - see the license file for more information

using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using libloc.Abstractions;
using libloc.V1.Collections;

namespace libloc.V1
{
    internal class DatabaseV1NetworkTreeEnumerator : IEnumerator<IAddressLocatedNetwork>
    {
        private readonly AddressFamily? _family;

        private readonly byte[] _networkAddress = new byte[16];
        private readonly DatabaseV1Networks _networks;
        private readonly Stack<NodeStackItem> _networkStack = new(256);
        private readonly DatabaseV1NetworkTree _networkTree;

        private readonly HashSet<uint> _visitedNetworks;

        public DatabaseV1NetworkTreeEnumerator(DatabaseV1NetworkTree tree, DatabaseV1Networks networks, AddressFamily? family)
        {
            _family = family;
            _networkTree = tree;
            _networks = networks;

            _visitedNetworks = new HashSet<uint>(_networks.Count);

            Reset();
        }

        public bool MoveNext()
        {
            // perform depth-first traversal
            while (_networkStack.Count > 0)
            {
                var node = _networkStack.Pop();

                if (!_visitedNetworks.Add(node.Index))
                {
                    continue;
                }

                AddressUtils.SetAddressBit(_networkAddress, node.Depth > 0 ? node.Depth - 1 : 0, node.NodeOne ? 1 : 0);

                // get node from tree and push next nodes to stack
                var treeNode = _networkTree.ElementAt((int)BinaryUtils.EnsureEndianness(node.Index));

                if (treeNode.one > 0)
                {
                    _networkStack.Push(new NodeStackItem(treeNode.one, node.Depth + 1, true));
                }

                if (treeNode.zero > 0)
                {
                    _networkStack.Push(new NodeStackItem(treeNode.zero, node.Depth + 1, false));
                }

                if (!treeNode.IsLeaf)
                {
                    continue;
                }

                // check if network is the right family
                if (_family.HasValue && AddressUtils.GetAddressFamily(_networkAddress) != _family)
                {
                    continue;
                }

                Current = _networks.CreateWithPrefix(BinaryUtils.EnsureEndianness(treeNode.network), _networkAddress, node.Depth);

                return true;
            }

            return false;
        }

        public void Reset()
        {
            _visitedNetworks.Clear();
            _networkStack.Clear();

            _networkStack.Push(new NodeStackItem(0, 0, false));
        }

        public IAddressLocatedNetwork Current { get; private set; }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }

        private readonly struct NodeStackItem
        {
            public NodeStackItem(uint index, int depth, bool nodeOne)
            {
                Index = index;
                Depth = depth;
                NodeOne = nodeOne;
            }

            public readonly uint Index;

            public readonly int Depth;
            public readonly bool NodeOne;
        }
    }
}