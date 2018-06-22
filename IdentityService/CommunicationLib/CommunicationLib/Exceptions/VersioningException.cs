using System;
using System.Net;
using System.Runtime.Serialization;

namespace Communication.Exceptions
{
    /// <summary>
    /// An exception that occured during a HTTP-request about versioning
    /// </summary>
    [Serializable]
    public class VersioningException : ServiceException
    {
        public VersioningException()
            : base(HttpStatusCode.BadRequest, "Bad Request")
        {
        }

        public VersioningException(string message)
            : base(HttpStatusCode.BadRequest, message)
        {
        }

        public VersioningException(string message, Exception innerException)
            : base(HttpStatusCode.BadRequest, message, innerException)
        {
        }

        protected VersioningException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
