// liblocsharp - A version of IPFire's libloc library rewritten for C#
// Licensed under LGPL-2.1 - see the license file for more information

using System;
using DragonFruit.Data;
using DragonFruit.Data.Extensions;
using DragonFruit.Data.Requests;

namespace libloc.Access
{
    public class LocationDbDownloadRequest : ApiRequest, IRequestExecutingCallback
    {
        public override string Path => "https://location.ipfire.org/databases/1/location.db.xz";

        /// <summary>
        /// The <see cref="DateTime"/> the previous database was downloaded, if applicable
        /// </summary>
        public DateTime? LastDownload { get; set; }

        void IRequestExecutingCallback.OnRequestExecuting(ApiClient client)
        {
            if (LastDownload.HasValue)
            {
                this.WithHeader("If-Modified-Since", LastDownload.Value.ToString("R"));
            }
        }
    }
}