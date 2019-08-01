using System;

namespace AcsListener
{
    public class AcspInvalidResponseException : Exception
    {
        public AcspInvalidResponseException()
        {
        }

        public AcspInvalidResponseException(string message) : base(message)
        {
        }

        public AcspInvalidResponseException(string message, Exception inner) : base (message, inner)
        {
        }
    }
}
