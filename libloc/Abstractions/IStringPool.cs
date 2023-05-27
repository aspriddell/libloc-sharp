// liblocsharp - A version of IPFire's libloc library rewritten for C#
// Licensed under LGPL-2.1 - see the license file for more information

namespace libloc.Abstractions
{
    internal interface IStringPool
    {
        string this[uint offset] { get; }
    }
}