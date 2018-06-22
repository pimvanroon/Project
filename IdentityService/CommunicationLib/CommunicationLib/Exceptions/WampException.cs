using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Communication.Exceptions
{
    /// <summary>
    /// An exception that occured handling a WAMP-request
    /// </summary>
    [Serializable]
    public class WampException : Exception
    {
        public string ResourceReferenceProperty { get; set; }

        public WampException()
            : base()
        {
        }

        public WampException(string message)
            : base(message)
        {
        }

        public WampException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected WampException(SerializationInfo info, StreamingContext context)
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
