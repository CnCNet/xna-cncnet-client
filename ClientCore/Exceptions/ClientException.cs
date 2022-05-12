using System;

namespace ClientCore.Exceptions
{
    public class ClientException : Exception
    {
        public ClientException(string message, Exception innerException = null) : base(message, innerException)
        {
        }
    }
}
