// liblocsharp - A version of IPFire's libloc library rewritten for C#
// Licensed under LGPL-2.1 - see the license file for more information

using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using libloc.Abstractions;
using libloc.Exceptions;
using libloc.V1;

namespace libloc
{
    public static class DatabaseLoader
    {
        private const string MagicName = "LOCDBXX";

        public static unsafe ILocationDatabase LoadFromFile(string path)
        {
            var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 0x1000, FileOptions.RandomAccess);
            var file = MemoryMappedFile.CreateFromFile(fileStream, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, false);

            try
            {
                GlobalDatabaseHeader magicValue;

                using (var headerView = file.CreateViewAccessor(0, sizeof(GlobalDatabaseHeader), MemoryMappedFileAccess.Read))
                {
                    headerView.Read(0, out magicValue);
                }

                if (Encoding.ASCII.GetString(magicValue.magic, GlobalDatabaseHeader.MagicByteLength) != MagicName)
                {
                    throw new FileFormatException("The file does not use the supported format");
                }

                switch (magicValue.version)
                {
                    case 1:
                        return new DatabaseV1(file);

                    default:
                        throw new FileVersionException(magicValue.version);
                }
            }
            catch
            {
                // dispose before throwing
                file.Dispose();

                throw;
            }
        }
    }
}