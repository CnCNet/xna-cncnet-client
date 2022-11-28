using System.Net;

namespace ClientCore.Exceptions
{
    public class ClientRequestException : ClientException
    {
        public HttpStatusCode? StatusCode { get; }

        public ClientRequestException(string message, HttpStatusCode? statusCode = null) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
