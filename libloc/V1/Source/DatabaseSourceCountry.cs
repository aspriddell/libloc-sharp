// liblocsharp - A version of IPFire's libloc library rewritten for C#
// Licensed under LGPL-2.1 - see the license file for more information

using System.Runtime.InteropServices;

namespace libloc.V1.Source
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct DatabaseSourceCountry
    {
        public fixed byte code[2];

        public fixed byte continent_code[2];

        public uint name_poolid;
    }
}