using System;
using System.Runtime.Serialization;

namespace ImageMerger.Exceptions
{
    [Serializable]
    internal class ImageFileNotFoundException : Exception
    {
        public ImageFileNotFoundException()
        {
        }

        public ImageFileNotFoundException(string message) : base(message)
        {
        }

        public ImageFileNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ImageFileNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
