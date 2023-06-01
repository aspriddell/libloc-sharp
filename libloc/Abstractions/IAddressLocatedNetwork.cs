// liblocsharp - A version of IPFire's libloc library rewritten for C#
// Licensed under LGPL-2.1 - see the license file for more information

using System.Net;

namespace libloc.Abstractions
{
    public interface IAddressLocatedNetwork : IDatabaseNetwork
    {
        IPNetwork Network { get; }
    }
}