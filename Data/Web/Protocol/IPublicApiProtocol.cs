﻿// Pixeval
// Copyright (C) 2019 Dylech30th <decem0730@gmail.com>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.Threading.Tasks;
using Pixeval.Data.Web.Request;
using Pixeval.Data.Web.Response;
using Refit;

namespace Pixeval.Data.Web.Protocol
{
    [Headers("Authorization: Bearer")]
    public interface IPublicApiProtocol
    {
        [Get("/search/works.json")]
        Task<QueryResponse> QueryWorks(QueryWorksRequest queryWorksRequest);

        [Get("/users/{uid}/works.json")]
        Task<UploadResponse> GetUploads(string uid, UploadsRequest uploadResponse);

        [Get("/works/{uid}.json")]
        Task<IllustResponse> GetSingle(string uid, [AliasAs("image_sizes")] string imageSizes = "px_128x128,small,medium,large,px_480mw", [AliasAs("include_stats")] string includeStat = "true");
    }
}