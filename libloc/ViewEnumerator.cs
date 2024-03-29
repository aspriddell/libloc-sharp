﻿// libloc-sharp - A version of IPFire's libloc library rewritten for .NET
// Licensed under LGPL-2.1 - see the license file for more information

using System.Collections;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;

namespace libloc
{
    internal class ViewEnumerator<T> : IEnumerator<T> where T : struct
    {
        private readonly MemoryMappedViewAccessor _accessor;
        private readonly int _chunkSize;
        private readonly int _maxOffset;
        private readonly int _originalOffset;
        private T _current;

        private int _offset;

        internal ViewEnumerator(MemoryMappedViewAccessor accessor, int skip = 0, int? count = null)
        {
            _accessor = accessor;

            _chunkSize = Unsafe.SizeOf<T>();
            _originalOffset = skip * _chunkSize;
            _maxOffset = _originalOffset + count * _chunkSize ?? (int)accessor.Capacity;

            Reset();
        }

        public bool MoveNext()
        {
            if (_offset >= _maxOffset)
            {
                return false;
            }

            _accessor.Read(_offset, out _current);
            _offset += _chunkSize;

            return true;
        }

        public void Reset()
        {
            _offset = _originalOffset;
            _current = default;
        }

        public T Current => _current;

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            // disposal of view handler is managed elsewhere
        }
    }
}