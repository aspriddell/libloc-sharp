// liblocsharp - A version of IPFire's libloc library rewritten for C#
// Licensed under LGPL-2.1 - see the license file for more information

using System.Collections.Generic;

namespace libloc.Abstractions
{
    public interface IASDatabase : IReadOnlyList<IDatabaseAS>
    {
        IDatabaseAS GetAS(int asn);
    }
}