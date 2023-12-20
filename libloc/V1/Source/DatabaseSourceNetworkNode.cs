// libloc-sharp - A version of IPFire's libloc library rewritten for .NET
// Licensed under LGPL-2.1 - see the license file for more information

using System.Runtime.InteropServices;

namespace libloc.V1.Source
{
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct DatabaseSourceNetworkNode
    {
        // The node to checkout if the next bit in the series is zero
        public readonly uint zero;

        // The node to checkout if the next bit in the series is one
        public readonly uint one;

        public readonly uint network;

        internal bool IsLeaf => network != 0xffffffff;
    }
}