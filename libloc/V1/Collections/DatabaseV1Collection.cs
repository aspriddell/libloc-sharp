// libloc-sharp - A version of IPFire's libloc library rewritten for .NET
// Licensed under LGPL-2.1 - see the license file for more information

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;

namespace libloc.V1.Collections
{
    internal abstract class DatabaseV1Collection<T> : IEnumerable<T>, IDisposable where T : struct
    {
        private readonly int _entitySize;
        private readonly MemoryMappedViewAccessor _view;

        protected DatabaseV1Collection(MemoryMappedViewAccessor view)
        {
            _entitySize = Unsafe.SizeOf<T>();
            _view = view;

            Count = (int)_view.Capacity / _entitySize;
        }

        public int Count { get; }

        public virtual void Dispose()
        {
            _view.Dispose();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new ViewEnumerator<T>(_view);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }

        internal protected T ElementAt(int index)
        {
            _view.Read(index * _entitySize, out T data);
            return data;
        }
    }
}