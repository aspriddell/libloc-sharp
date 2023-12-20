// libloc-sharp - A version of IPFire's libloc library rewritten for .NET
// Licensed under LGPL-2.1 - see the license file for more information

using System;

namespace libloc.Exceptions
{
    public class FileFormatException : Exception
    {
        public FileFormatException(string message)
            : base(message)
        {
        }
    }
}