using System;
using System.Net;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// A web client that supports customizing the timeout of the request.
    /// </summary>
    class ExtendedWebClient : WebClient
    {
        public ExtendedWebClient() : this(timeout: 10000) { }

        public ExtendedWebClient(int timeout)
        {
            this.timeout = timeout;

            // Interferes with POST requests to API if left enabled
            // https://learn.microsoft.com/dotnet/api/system.net.servicepointmanager.expect100continue
            ServicePointManager.Expect100Continue = false;
            // Increase default connection limit to allow a few concurrent requests
            ServicePointManager.DefaultConnectionLimit = 5; // Default is 2
        }

        private int timeout;

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest webRequest = base.GetWebRequest(address);
            webRequest.Timeout = timeout;
            return webRequest;
        }
    }
}
