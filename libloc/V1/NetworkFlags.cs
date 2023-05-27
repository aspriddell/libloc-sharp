// liblocsharp - A version of IPFire's libloc library rewritten for C#
// Licensed under LGPL-2.1 - see the license file for more information

namespace libloc.V1
{
    public enum NetworkFlags : ushort
    {
        AnonymousProxy = 1 << 0, // A1
        SatelliteProvider = 1 << 1, // A2
        Anycast = 1 << 2, // A3
        Drop = 1 << 3 // XD
    }
}