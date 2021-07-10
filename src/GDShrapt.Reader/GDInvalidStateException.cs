using System;
using System.Runtime.Serialization;

namespace GDShrapt.Reader
{
    public class GDInvalidStateException : Exception
    {
        public GDInvalidStateException()
        {
        }

        public GDInvalidStateException(string message) 
            : base(message)
        {
        }

        public GDInvalidStateException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected GDInvalidStateException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
