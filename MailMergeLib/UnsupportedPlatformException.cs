using System;

namespace MailMergeLib
{
    /// <summary>
    /// Represents when we don't support the current OS Platform.
    /// </summary>
    public class UnsupportedPlatformException : Exception
    {
        public UnsupportedPlatformException(string message) : base(message)
        {
        }
    }
}