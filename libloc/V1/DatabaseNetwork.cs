// libloc-sharp - A version of IPFire's libloc library rewritten for .NET
// Licensed under LGPL-2.1 - see the license file for more information

using System.Net;
using libloc.Abstractions;

namespace libloc.V1
{
    public record DatabaseNetwork(IPNetwork Network, string CountryCode, uint ASN, NetworkFlags Flags) : IAddressLocatedNetwork;
}