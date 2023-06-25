using System;
using System.Collections.Generic;
using MimeKit;

namespace MailMergeLib;

public partial class MailMergeMessage
{
    #region Nested type: AddressException

    /// <summary>
    ///  Mail merge bad address exception.
    /// </summary>
    public class AddressException : Exception
    {
        /// <summary>
        /// CTOR
        /// </summary>
        public AddressException(string message, HashSet<string> badAddress, Exception? innerException)
            : base(message, innerException)
        {
            BadAddress = badAddress;
        }

        /// <summary>
        /// Gets the bad email address(es) leading to the exception.
        /// </summary>
        public HashSet<string> BadAddress { get; }
    }

    #endregion

    #region Nested type: AttachmentException

    /// <summary>
    /// Mail merge attachment exception.
    /// </summary>
    public class AttachmentException : Exception
    {
        /// <summary>
        /// CTOR.
        /// </summary>
        public AttachmentException(string message, HashSet<string> badAttachment, Exception? innerException)
            : base(message, innerException)
        {
            BadAttachment = badAttachment;
        }

        /// <summary>
        /// Gets the list of bad attachments that caused the exception.
        /// </summary>
        public HashSet<string> BadAttachment { get; }
    }

    #endregion

    #region Nested type: EmtpyContentException

    /// <summary>
    /// Mail merge empty content exception.
    /// </summary>
    public class EmptyContentException : Exception
    {
        /// <summary>
        /// CTOR.
        /// </summary>
        public EmptyContentException(string message, Exception? innerException)
            : base(message, innerException)
        {
        }
    }

    #endregion

    #region Nested type: MailMergeMessageException

    /// <summary>
    /// Mail merge message exception.
    /// </summary>
    public class MailMergeMessageException : AggregateException
    {
        /// <summary>
        /// CTOR.
        /// </summary>
        public MailMergeMessageException(string message, IEnumerable<Exception> exceptions, MimeMessage? mimeMessage)
            : base(message, exceptions)
        {
            MimeMessage = mimeMessage;
        }

        /// <summary>
        /// Gets all exceptions that were thrown when the message was created.
        /// Check <see cref="AggregateException.InnerExceptions"/> for more details.
        /// </summary>
        public AggregateException? Exception { get; }

        /// <summary>
        /// Gets the <see cref="MimeMessage"/> where the exception was thrown.
        /// </summary>
        public MimeMessage? MimeMessage { get; }
    }

    #endregion

    #region Nested type: VariableException

    /// <summary>
    /// Mail merge exception for placeholders that are missing in the datasource.
    /// </summary>
    public class VariableException : Exception
    {
        /// <summary>
        /// CTOR.
        /// </summary>
        public VariableException(string message, HashSet<string> missingVariable, Exception? innerException)
            : base(message, innerException)
        {
            MissingVariable = missingVariable;
        }

        /// <summary>
        /// Gets the missing variables that caused the exception.
        /// </summary>
        public HashSet<string> MissingVariable { get; }
    }

    #endregion

    #region Nested type: ParseException

    /// <summary>
    /// Mail merge exception for not properly formatted templates (e.g. missing closing brace).
    /// </summary>
    public class ParseException : Exception
    {
        /// <summary>
        /// CTOR.
        /// </summary>
        public ParseException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }

    #endregion
}
