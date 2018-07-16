using System;
using System.Runtime.Serialization;

namespace ImageMerger.Exceptions
{
    [Serializable]
    internal class InvalidSettingsFileException : Exception
    {
        public InvalidSettingsFileException()
        {
        }

        public InvalidSettingsFileException(string message) : base(message)
        {
        }

        public InvalidSettingsFileException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidSettingsFileException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}