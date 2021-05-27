using System;
using System.Runtime.Serialization;

namespace Echse.Net.Infrastructure
{
    [Serializable]
    internal class NoValidNetworkCommandException : Exception
    {
        public NoValidNetworkCommandException()
        {
        }

        public NoValidNetworkCommandException(string message) : base(message)
        {
        }

        public NoValidNetworkCommandException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NoValidNetworkCommandException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}