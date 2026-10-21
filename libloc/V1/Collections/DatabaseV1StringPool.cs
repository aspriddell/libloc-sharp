// libloc-sharp - A version of IPFire's libloc library rewritten for .NET
// Licensed under LGPL-2.1 - see the license file for more information

using System;
using System.IO.MemoryMappedFiles;
using System.Text;
using libloc.Abstractions;
using libloc.Exceptions;

namespace libloc.V1.Collections
{
    internal class DatabaseV1StringPool : IStringPool, IDisposable
    {
        private readonly MemoryMappedViewAccessor _sourcePool;

        internal DatabaseV1StringPool(MemoryMappedViewAccessor sourcePool)
        {
            _sourcePool = sourcePool;

            if (_sourcePool.Capacity == 0 || _sourcePool.ReadByte(_sourcePool.Capacity - 1) != 0)
            {
                throw new FileFormatException("The string pool is not terminated");
            }
        }

        public void Dispose()
        {
            _sourcePool.Dispose();
        }

        public string this[uint offset] => GetInternal(offset);

        private unsafe string GetInternal(uint offset)
        {
            var correctedOffset = BinaryUtils.EnsureEndianness(offset);
            if (correctedOffset >= _sourcePool.Capacity)
            {
                throw new FileFormatException("The string offset is outside the string pool");
            }

            byte* ptr = null;

            _sourcePool.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);

            try
            {
                var remaining = (int)Math.Min(_sourcePool.Capacity - correctedOffset, int.MaxValue);
                var bytes = new ReadOnlySpan<byte>(ptr + _sourcePool.PointerOffset + correctedOffset, remaining);
                var length = bytes.IndexOf((byte)0);
                if (length < 0)
                {
                    throw new FileFormatException("The string is not terminated within the supported length");
                }

                return Encoding.UTF8.GetString(bytes.Slice(0, length));
            }
            finally
            {
                _sourcePool.SafeMemoryMappedViewHandle.ReleasePointer();
            }
        }
    }
}