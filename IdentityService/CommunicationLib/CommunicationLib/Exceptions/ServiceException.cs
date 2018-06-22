using System;
using System.Net;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Communication.Exceptions
{
    /// <summary>
    /// A generic ServiceException, containing a HTTP statuscode
    /// It's message should be aimed to the public that uses the web service.
    /// Should be used in a 'catch' all handler when handling a HTTP-request, so
    /// the statuscode can be reported in the response.
    /// </summary>
    [Serializable]
    public class ServiceException : Exception
    {
        public string ResourceReferenceProperty { get; set; }

        /// <summary>
        /// The HTTP statuscode that should be reported in the response
        /// </summary>
        public HttpStatusCode HttpStatusCode { get; private set; }

        public ServiceException(HttpStatusCode httpStatusCode, string message)
            : base(message)
        {
            this.HttpStatusCode = httpStatusCode;
        }

        public ServiceException(HttpStatusCode httpStatusCode, string message, Exception innerException)
            : base(message, innerException)
        {
            this.HttpStatusCode = httpStatusCode;
        }

        protected ServiceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ResourceReferenceProperty = info.GetString("ResourceReferenceProperty");
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            info.AddValue("ResourceReferenceProperty", ResourceReferenceProperty);
            base.GetObjectData(info, context);
        }
    }
}
