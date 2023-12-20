// libloc-sharp - A version of IPFire's libloc library rewritten for .NET
// Licensed under LGPL-2.1 - see the license file for more information

using System;
using System.Globalization;
using DragonFruit.Data;
using DragonFruit.Data.Requests;

namespace libloc.Access
{
    public partial class LocationDbDownloadRequest : ApiRequest
    {
        public override string RequestPath => "https://location.ipfire.org/databases/1/location.db.xz";

        /// <summary>
        /// The <see cref="DateTime"/> the previous database was downloaded, if applicable
        /// </summary>
        public DateTimeOffset? LastDownload { get; init; }

        [RequestParameter(ParameterType.Header, "If-Modified-Since")]
        protected string ModificationDate => LastDownload?.ToString("r", CultureInfo.InvariantCulture);
    }
}