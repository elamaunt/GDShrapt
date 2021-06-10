using System;
using System.Runtime.Serialization;

namespace GDShrapt.Reader
{
    public class GDInvalidReadingStateException : Exception
    {
        public GDInvalidReadingStateException()
        {
        }

        public GDInvalidReadingStateException(string message) 
            : base(message)
        {
        }

        public GDInvalidReadingStateException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected GDInvalidReadingStateException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
