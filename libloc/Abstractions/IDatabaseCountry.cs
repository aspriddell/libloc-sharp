// libloc-sharp - A version of IPFire's libloc library rewritten for .NET
// Licensed under LGPL-2.1 - see the license file for more information

namespace libloc.Abstractions
{
    public interface IDatabaseCountry
    {
        string Name { get; }
        string Code { get; }
        string ContinentCode { get; }
    }
}