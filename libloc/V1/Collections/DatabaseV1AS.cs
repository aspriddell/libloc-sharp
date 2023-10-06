// liblocsharp - A version of IPFire's libloc library rewritten for C#
// Licensed under LGPL-2.1 - see the license file for more information

using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using libloc.Abstractions;
using libloc.V1.Source;

namespace libloc.V1.Collections
{
    internal class DatabaseV1AS : DatabaseV1Collection<DatabaseSourceAS>, IASDatabase
    {
        private readonly IStringPool _pool;

        public DatabaseV1AS(MemoryMappedViewAccessor view, IStringPool pool)
            : base(view)
        {
            _pool = pool;
        }

        public IDatabaseAS this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new ArgumentOutOfRangeException($"Index must be positive and less than the {nameof(Count)}");
                }

                return FromSource(ElementAt(index));
            }
        }

        public IDatabaseAS GetAS(int asn) => BinaryUtils.BinarySearch(Count, x => FromSource(ElementAt(x)), asn);

        private DatabaseAS FromSource(DatabaseSourceAS source)
        {
            var asn = BinaryUtils.EnsureEndianness(source.number);
            return new DatabaseAS((int)asn, _pool[source.name_poolid]);
        }

        public IEnumerator<IDatabaseAS> GetEnumerator()
        {
            return ((IEnumerable<DatabaseSourceAS>)this).Select(FromSource).GetEnumerator();
        }
    }
}