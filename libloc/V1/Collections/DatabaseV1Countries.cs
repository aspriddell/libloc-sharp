// liblocsharp - A version of IPFire's libloc library rewritten for C#
// Licensed under LGPL-2.1 - see the license file for more information

using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using libloc.Abstractions;
using libloc.V1.Source;

namespace libloc.V1.Collections
{
    internal unsafe class DatabaseV1Countries : DatabaseV1Collection<DatabaseSourceCountry>, ICountryDatabase
    {
        private readonly IStringPool _pool;

        public DatabaseV1Countries(MemoryMappedViewAccessor view, IStringPool pool)
            : base(view)
        {
            _pool = pool;
        }

        public IDatabaseCountry this[int index] => FromSource(ElementAt(index));

        public IDatabaseCountry GetCountry(string code)
        {
            return BinaryUtils.BinarySearch(Count, x => FromSource(ElementAt(x)), code);
        }

        private DatabaseCountry FromSource(DatabaseSourceCountry source)
        {
            var code = Encoding.ASCII.GetString(source.code, 2);
            var continent = Encoding.ASCII.GetString(source.continent_code, 2);

            return new DatabaseCountry(code, continent, _pool[source.name_poolid]);
        }

        public IEnumerator<IDatabaseCountry> GetEnumerator()
        {
            return ((IEnumerable<DatabaseSourceCountry>)this).Select(FromSource).GetEnumerator();
        }
    }
}