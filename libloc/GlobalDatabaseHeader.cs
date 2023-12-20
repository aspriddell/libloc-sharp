// libloc-sharp - A version of IPFire's libloc library rewritten for .NET
// Licensed under LGPL-2.1 - see the license file for more information

using System.Runtime.InteropServices;

namespace libloc
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct GlobalDatabaseHeader
    {
        internal const int MagicByteLength = 7;

        public fixed byte magic[MagicByteLength];
        public readonly byte version;
    }
}