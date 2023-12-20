// libloc-sharp - A version of IPFire's libloc library rewritten for .NET
// Licensed under LGPL-2.1 - see the license file for more information

using System.Collections.Generic;

namespace libloc.Abstractions
{
    public interface IASDatabase : IReadOnlyList<IDatabaseAS>
    {
        IDatabaseAS GetAS(int asn);
    }
}