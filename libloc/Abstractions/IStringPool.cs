// libloc-sharp - A version of IPFire's libloc library rewritten for .NET
// Licensed under LGPL-2.1 - see the license file for more information

namespace libloc.Abstractions
{
    internal interface IStringPool
    {
        string this[uint offset] { get; }
    }
}