using System;
using System.Net;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// A web client that supports customizing the timeout of the request.
    /// </summary>
    class ExtendedWebClient : WebClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedWebClient"/> class with a default timeout of 10 seconds.
        /// </summary>
        public ExtendedWebClient() : this(timeout: 10000) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedWebClient"/> class with a specified timeout.
        /// </summary>
        /// <param name="timeout">Gets or sets the length of time, in milliseconds, before the request times out.</param>
        public ExtendedWebClient(int timeout)
        {
            this.timeout = timeout;
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
