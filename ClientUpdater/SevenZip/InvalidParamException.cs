using System;

namespace SevenZip
{
    internal class InvalidParamException : ApplicationException
    {
        public InvalidParamException()
            : base("Invalid Parameter")
        {
        }
    }
}