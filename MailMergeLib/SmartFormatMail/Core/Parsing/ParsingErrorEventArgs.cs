using System;

namespace MailMergeLib.SmartFormatMail.Core.Parsing
{
    /// <summary>
    /// Supplies information about parsing errors.
    /// </summary>
    [System.Obsolete("Use classes in namespace 'SmartFormat' instead of 'MailMergeLib.SmartFormatMail'", false)] public class ParsingErrorEventArgs : EventArgs
    {
        internal ParsingErrorEventArgs(ParsingErrors errors, bool throwsException)
        {
            Errors = errors;
            ThrowsException = throwsException;
        }

        /// <summary>
        /// All parsing errors which occurred during parsing.
        /// </summary>
        public ParsingErrors Errors { get; internal set; }

        /// <summary>
        /// If true, the errors will throw an exception.
        /// </summary>
        public bool ThrowsException { get; internal set; }
    }
}