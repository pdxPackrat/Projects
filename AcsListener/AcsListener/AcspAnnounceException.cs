using System;

namespace AcsListener
{
    class AcspAnnounceException : Exception
    {
        public AcspAnnounceException()
        {
        }

        public AcspAnnounceException(string message) : base(message)
        {
        }

        public AcspAnnounceException(string message, Exception inner) : base(message,inner)
        {
        }
    }
}
