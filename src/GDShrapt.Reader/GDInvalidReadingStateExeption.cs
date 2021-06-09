using System;
using System.Runtime.Serialization;

namespace GDShrapt.Reader
{
    public class GDInvalidReadingStateExeption : Exception
    {
        public GDInvalidReadingStateExeption()
        {
        }

        public GDInvalidReadingStateExeption(string message) 
            : base(message)
        {
        }

        public GDInvalidReadingStateExeption(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected GDInvalidReadingStateExeption(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
