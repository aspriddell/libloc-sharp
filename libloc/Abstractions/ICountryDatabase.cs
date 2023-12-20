﻿// libloc-sharp - A version of IPFire's libloc library rewritten for .NET
// Licensed under LGPL-2.1 - see the license file for more information

using System.Collections.Generic;

namespace libloc.Abstractions
{
    public interface ICountryDatabase : IReadOnlyList<IDatabaseCountry>
    {
        IDatabaseCountry GetCountry(string code);
    }
}