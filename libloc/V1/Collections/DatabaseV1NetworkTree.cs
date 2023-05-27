// liblocsharp - A version of IPFire's libloc library rewritten for C#
// Licensed under LGPL-2.1 - see the license file for more information

using System.IO.MemoryMappedFiles;
using libloc.V1.Source;

namespace libloc.V1.Collections
{
    internal class DatabaseV1NetworkTree : DatabaseV1Collection<DatabaseSourceNetworkNode>
    {
        public DatabaseV1NetworkTree(MemoryMappedViewAccessor view)
            : base(view)
        {
        }
    }
}