// liblocsharp - A version of IPFire's libloc library rewritten for C#
// Licensed under LGPL-2.1 - see the license file for more information

using System;
using System.Threading.Tasks;
using libloc.Abstractions;

namespace libloc.Access
{
    public interface IDatabaseAccessor
    {
        Task<T> PerformAsync<T>(Func<ILocationDatabase, T> action);
    }
}