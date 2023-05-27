// liblocsharp - A version of IPFire's libloc library rewritten for C#
// Licensed under LGPL-2.1 - see the license file for more information

using System.Diagnostics;
using libloc.Abstractions;

namespace libloc.V1
{
    [DebuggerDisplay("{Code} - {Name}")]
    internal record DatabaseCountry(string Code, string ContinentCode, string Name) : IDatabaseCountry, ISearchableItem<string>
    {
        string ISearchableItem<string>.Key => Code;
    }
}