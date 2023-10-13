// liblocsharp - A version of IPFire's libloc library rewritten for C#
// Licensed under LGPL-2.1 - see the license file for more information

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace libloc.Abstractions
{
    public interface ILocationDatabase : IEnumerable<IAddressLocatedNetwork>, IDisposable
    {
        int Version { get; }

        string Vendor { get; }
        string License { get; }
        string Description { get; }

        DateTimeOffset CreatedAt { get; }

        IASDatabase AS { get; }
        INetworkDatabase Networks { get; }
        ICountryDatabase Countries { get; }

        IAddressLocatedNetwork ResolveAddress(IPAddress address);
        IEnumerator<IAddressLocatedNetwork> GetEnumerator(AddressFamily family, bool flattened);
    }
}