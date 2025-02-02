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

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Pixeval.Objects;
using Pixeval.Persisting;

namespace Pixeval.Data.Web.Delegation
{
    public abstract class DnsResolvedHttpClientHandler : HttpClientHandler
    {
        private readonly IHttpRequestHandler requestHandler;

        static DnsResolvedHttpClientHandler()
        {
            // use legacy http handler instead of .net core's new http handler, that will break whole program on SSL check
            AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", false);
        }

        protected DnsResolvedHttpClientHandler(IHttpRequestHandler requestHandler = null)
        {
            this.requestHandler = requestHandler;
            // SSL bypass
            ServerCertificateCustomValidationCallback = DangerousAcceptAnyServerCertificateValidator;
        }

        protected abstract DnsResolver DnsResolver { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var host = request.RequestUri.DnsSafeHost;

            var isSslSession = request.RequestUri.ToString().StartsWith("https://");

            // replace the host part in the uri to the ip address of the host
            request.RequestUri = new Uri($"{(isSslSession ? "https://" : "http://")}{(await DnsResolver.Lookup(host))[0]}{request.RequestUri.PathAndQuery}");
            request.Headers.Host = host;

            requestHandler?.Handle(request);

            HttpResponseMessage result;
            try
            {
                result = await base.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException e)
            {
                // this exception throws several times with form like "Error Code 12152 WinHttpException" and I cannot find reason, so I just ignore it
                if (e.InnerException != null && e.InnerException.Message.ToLower().Contains("winhttp"))
                    return new HttpResponseMessage(HttpStatusCode.OK);
                throw;
            }

            // bad request with authorization messages means the token has been expired, we will refresh the token and try to send request again
            if (result.StatusCode == HttpStatusCode.BadRequest && (await result.Content.ReadAsStringAsync()).Contains("OAuth"))
            {
                using var _ = await new AsyncLock().LockAsync();
                await Authentication.Authenticate(Identity.Global.MailAddress, Identity.Global.Password);

                var token = request.Headers.Authorization;
                if (token != null)
                    request.Headers.Authorization = new AuthenticationHeaderValue(token.Scheme, Identity.Global.AccessToken);

                return await base.SendAsync(request, cancellationToken);
            }

            return result;
        }
    }
}