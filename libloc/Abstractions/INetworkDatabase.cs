// liblocsharp - A version of IPFire's libloc library rewritten for C#
// Licensed under LGPL-2.1 - see the license file for more information

namespace libloc.Abstractions
{
    public interface INetworkDatabase
    {
        IDatabaseNetwork this[uint index] { get; }
    }
}