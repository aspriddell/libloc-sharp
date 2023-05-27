// liblocsharp - A version of IPFire's libloc library rewritten for C#
// Licensed under LGPL-2.1 - see the license file for more information

using System.Diagnostics;
using libloc.Abstractions;

namespace libloc.V1
{
    [DebuggerDisplay("AS{Number} - {Name}")]
    internal record DatabaseAS(uint Number, string Name) : IDatabaseAS, ISearchableItem<uint>
    {
        uint ISearchableItem<uint>.Key => Number;

        public override string ToString() => $"AS{Number}";
    }
}