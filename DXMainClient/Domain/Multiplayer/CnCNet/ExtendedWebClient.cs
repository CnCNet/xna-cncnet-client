using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// A web client that supports customizing the timeout of the request.
    /// </summary>
    class ExtendedWebClient : WebClient
    {
        public ExtendedWebClient(int timeout)
        {
            this.timeout = timeout;

            // Inteferes with post requests to API
            // https://docs.microsoft.com/en-us/dotnet/api/system.net.servicepointmanager.expect100continue?view=netframework-4.8

            ServicePointManager.Expect100Continue = false; 
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
