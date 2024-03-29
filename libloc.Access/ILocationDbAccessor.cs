// libloc-sharp - A version of IPFire's libloc library rewritten for .NET
// Licensed under LGPL-2.1 - see the license file for more information

using System;
using System.Threading.Tasks;
using libloc.Abstractions;

namespace libloc.Access
{
    public interface ILocationDbAccessor
    {
        Task PerformAsync(Func<ILocationDatabase, Task> action);

        Task<T> PerformAsync<T>(Func<ILocationDatabase, T> action);
        Task<T> PerformAsync<T>(Func<ILocationDatabase, Task<T>> action);
    }
}