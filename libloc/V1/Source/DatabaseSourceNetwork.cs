// liblocsharp - A version of IPFire's libloc library rewritten for C#
// Licensed under LGPL-2.1 - see the license file for more information

using System.Runtime.InteropServices;

namespace libloc.V1.Source
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct DatabaseSourceNetwork
    {
        // The start address and prefix will be encoded in the tree

        // The country this network is located in
        public fixed byte country_code[2];

        // ASN\\t
        public readonly uint asn;

        // Flags
        public readonly ushort flags;

        // Reserved
        private fixed byte padding[2];
    }
}