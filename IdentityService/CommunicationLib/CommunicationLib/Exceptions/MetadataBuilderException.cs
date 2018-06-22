using System;
using System.Net;
using System.Runtime.Serialization;

namespace Communication.Exceptions
{
    /// <summary>
    /// An exception occured in the generation of Metadata
    /// </summary>
    [Serializable]
    public class MetadataBuilderException : ServiceException
    {
        public MetadataBuilderException()
            : base(HttpStatusCode.InternalServerError, "Metadata error")
        {
        }

        public MetadataBuilderException(string message)
            : base(HttpStatusCode.InternalServerError, message)
        {
        }

        public MetadataBuilderException(string message, Exception innerException)
            : base(HttpStatusCode.InternalServerError, message, innerException)
        {
        }

        protected MetadataBuilderException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
