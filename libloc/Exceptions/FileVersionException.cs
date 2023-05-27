// liblocsharp - A version of IPFire's libloc library rewritten for C#
// Licensed under LGPL-2.1 - see the license file for more information

using System;

namespace libloc.Exceptions
{
    public class FileVersionException : Exception
    {
        public FileVersionException(int version)
            : base($"The provided file is not currently supported (version {version})")
        {
        }
    }
}