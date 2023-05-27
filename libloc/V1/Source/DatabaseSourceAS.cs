// liblocsharp - A version of IPFire's libloc library rewritten for C#
// Licensed under LGPL-2.1 - see the license file for more information

using System.Runtime.InteropServices;

namespace libloc.V1.Source
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct DatabaseSourceAS
    {
        // The AS number
        public readonly uint number;

        // Name
        public readonly uint name_poolid;
    }
}