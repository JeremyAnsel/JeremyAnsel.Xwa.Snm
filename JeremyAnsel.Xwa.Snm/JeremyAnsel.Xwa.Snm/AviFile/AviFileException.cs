using System;
using System.Runtime.Serialization;

namespace AviFile
{
    [Serializable]
    public class AviFileException : Exception
    {
        public AviFileException() : base()
        {
        }

        public AviFileException(string message) : base(message)
        {
        }

        public AviFileException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected AviFileException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
