using System;
using System.Collections.Generic;
using MimeKit;

namespace MailMergeLib;

partial class MailMergeMessage
{
    #region Nested type: AddressException

    /// <summary>
    ///  Mail merge bad address exception.
    /// </summary>
    public class AddressException : Exception
    {
        public AddressException(string message, HashSet<string> badAddress, Exception innerException)
            : base(message, innerException)
        {
            BadAddress = badAddress;
        }

        public HashSet<string> BadAddress { get; } = new HashSet<string>();
    }

    #endregion

    #region Nested type: AttachmentException

    /// <summary>
    /// Mail merge attachment exception.
    /// </summary>
    public class AttachmentException : Exception
    {
        public AttachmentException(string message, HashSet<string> badAttachment, Exception innerException)
            : base(message, innerException)
        {
            BadAttachment = badAttachment;
        }

        public HashSet<string> BadAttachment { get; } = new HashSet<string>();
    }

    #endregion

    #region Nested type: EmtpyContentException

    /// <summary>
    /// Mail merge empty content exception.
    /// </summary>
    public class EmtpyContentException : Exception
    {
        public EmtpyContentException(string message, Exception innerException)
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
        public MailMergeMessageException(string message, IEnumerable<Exception> exceptions, MimeMessage mimeMessage)
            : base(message, exceptions)
        {
            MimeMessage = mimeMessage;
        }

        public AggregateException Exception { get; }

        public MimeMessage MimeMessage { get; }
    }

    #endregion

    #region Nested type: VariableException

    /// <summary>
    /// Mail merge exception for placeholders that are missing in the datasource.
    /// </summary>
    public class VariableException : Exception
    {
        public VariableException(string message, HashSet<string> missingVariable, Exception innerException)
            : base(message, innerException)
        {
            MissingVariable = missingVariable;
        }

        public HashSet<string> MissingVariable { get; }
    }

    #endregion

    #region Nested type: ParseException

    /// <summary>
    /// Mail merge exception for not properly formatted templates (e.g. missing closing brace).
    /// </summary>
    public class ParseException : Exception
    {
        public ParseException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }

    #endregion
}