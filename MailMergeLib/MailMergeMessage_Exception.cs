using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using MimeKit;

namespace MailMergeLib
{
	public partial class MailMergeMessage
	{
		#region Nested type: AddressException

		/// <summary>
		///  Mail merge bad address exception.
		/// </summary>
		public class AddressException : Exception
		{
			public AddressException(string message, List<string> badAddress, Exception innerException)
				: base(message, innerException)
			{
				BadAddress = badAddress;
			}

			// necessary to ensure serialization is possible
			protected AddressException(SerializationInfo info, StreamingContext context)
				: base(info, context)
			{
			}

			public List<string> BadAddress { get; } = new List<string>();
		}

		#endregion

		#region Nested type: AttachmentException

		/// <summary>
		/// Mail merge attachment exception.
		/// </summary>
		public class AttachmentException : Exception
		{
			public AttachmentException(string message, List<string> badAttachment, Exception innerException)
				: base(message, innerException)
			{
				BadAttachment = badAttachment;
			}

			// necessary to ensure serialization is possible
			protected AttachmentException(SerializationInfo info, StreamingContext context)
				: base(info, context)
			{
			}

			public List<string> BadAttachment { get; } = new List<string>();
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

			// necessary to ensure serialization is possible
			protected EmtpyContentException(SerializationInfo info, StreamingContext context)
				: base(info, context)
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

			// necessary to ensure serialization is possible
			protected MailMergeMessageException(SerializationInfo info, StreamingContext context)
				: base(info, context)
			{
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
			public VariableException(string message, List<string> missingVariable, Exception innerException)
				: base(message, innerException)
			{
				MissingVariable = missingVariable;
			}

			// necessary to ensure serialization is possible
			protected VariableException(SerializationInfo info, StreamingContext context)
				: base(info, context)
			{
			}

			public List<string> MissingVariable { get; } = new List<string>();
		}

		#endregion
	}
}