// liblocsharp - A version of IPFire's libloc library rewritten for C#
// Licensed under LGPL-2.1 - see the license file for more information

using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using libloc.Abstractions;

namespace libloc.V1.Collections
{
    internal class DatabaseV1StringPool : IStringPool, IDisposable
    {
        private readonly MemoryMappedViewAccessor _sourcePool;

        internal DatabaseV1StringPool(MemoryMappedViewAccessor sourcePool)
        {
            _sourcePool = sourcePool;
        }

        public string this[uint offset] => GetInternal(offset);

        private unsafe string GetInternal(uint offset)
        {
            byte* ptr = null;

            _sourcePool.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);

            try
            {
                var correctedOffset = BinaryUtils.EnsureEndianness(offset);
                return Marshal.PtrToStringUTF8((IntPtr)ptr + (int)(_sourcePool.PointerOffset + correctedOffset));
            }
            finally
            {
                _sourcePool.SafeMemoryMappedViewHandle.ReleasePointer();
            }
        }

        public void Dispose()
        {
            _sourcePool.Dispose();
        }
    }
}