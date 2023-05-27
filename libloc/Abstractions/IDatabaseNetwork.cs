// liblocsharp - A version of IPFire's libloc library rewritten for C#
// Licensed under LGPL-2.1 - see the license file for more information

using libloc.V1;

namespace libloc.Abstractions
{
    public interface IDatabaseNetwork
    {
        string CountryCode { get; }

        uint ASN { get; }

        NetworkFlags Flags { get; }
    }
}