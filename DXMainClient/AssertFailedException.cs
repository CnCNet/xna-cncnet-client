#nullable enable
using System;

namespace DTAClient
{
    public class AssertFailedException : Exception
    {
        public AssertFailedException(string message) : base(message)
        {
        }
    }
}
