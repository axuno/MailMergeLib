using System;
using MimeKit;

namespace MailMergeLib
{
	/// <summary>
	/// Argument used by the event after sending of a message is completed.
	/// </summary>
	public class MailSenderAfterSendEventArgs : EventArgs
	{
		public readonly bool Cancelled;
		public readonly Exception Error;
		public readonly SmtpClientConfig SmtpClientConfig;
		public readonly MimeMessage MimeMessage;
		public readonly DateTime StartTime;
		public readonly DateTime EndTime;

		internal MailSenderAfterSendEventArgs(SmtpClientConfig smtpConfig, MimeMessage mailMergeMessage, DateTime startTime, DateTime endTime,
			Exception error, bool cancelled)
		{
			Error = error;
			Cancelled = cancelled;
			SmtpClientConfig = smtpConfig;
			MimeMessage = mailMergeMessage;
			StartTime = startTime;
			EndTime = endTime;
		}
	}

	/// <summary>
	/// Argument used by the event before sending of a message is completed.
	/// </summary>
	public class MailSenderBeforeSendEventArgs : EventArgs
	{
		public readonly bool Cancelled;
		public readonly Exception Error;
		public readonly SmtpClientConfig SmtpClientConfig;
		public readonly MimeMessage MimeMessage;
		public readonly DateTime StartTime;

		internal MailSenderBeforeSendEventArgs(SmtpClientConfig smtpConfig, MimeMessage mimeMessage, DateTime startTime, Exception error,
			bool cancelled)
		{
			Error = error;
			Cancelled = cancelled;
			MimeMessage = mimeMessage;
			SmtpClientConfig = smtpConfig;
			StartTime = startTime;
		}
	}

	/// <summary>
	/// Argument used by the event before starting a mail merge.
	/// </summary>
	public class MailSenderMergeBeginEventArgs : EventArgs
	{
		public readonly DateTime StartTime;
		public readonly int TotalMsg;

		internal MailSenderMergeBeginEventArgs(DateTime startTime, int totalMsg)
		{
			StartTime = startTime;
			TotalMsg = totalMsg;
		}
	}

	/// <summary>
	/// Argument used by the event after every mail sent during a mail merge.
	/// </summary>
	public class MailSenderMergeProgressEventArgs : MailSenderMergeBeginEventArgs
	{
		public readonly int SentMsg;
		public readonly int ErrorMsg;
		
		internal MailSenderMergeProgressEventArgs(DateTime startTime, int totalMsg, int sentMsg, int errorMsg) : base(startTime, totalMsg)
		{
			SentMsg = sentMsg;
			ErrorMsg = errorMsg;
		}
	}

	/// <summary>
	/// Argument used by the event after finishing a mail merge.
	/// </summary>
	public class MailSenderMergeCompleteEventArgs : MailSenderMergeProgressEventArgs
	{
		public readonly DateTime EndTime;
		public readonly int NumOfSmtpClientsUsed;

		internal MailSenderMergeCompleteEventArgs(DateTime startTime, DateTime endTime, int totalMsg, int sentMsg, int errorMsg, int numOfSmtpClientsUsed) : base(startTime, totalMsg, sentMsg, errorMsg)
		{
			EndTime = endTime;
			NumOfSmtpClientsUsed = numOfSmtpClientsUsed;
		}
	}

	/// <summary>
	/// Argument used by the event after sending a message has failed.
	/// </summary>
	public class MailSenderSendFailureEventArgs : EventArgs
	{
		public readonly Exception Error;
		public readonly int FailureCounter;
		public readonly MimeMessage MimeMessage;
		public readonly SmtpClientConfig SmtpClientConfig;

		internal MailSenderSendFailureEventArgs(Exception error, int failureCounter, SmtpClientConfig smtpClientConfig,
		                                        MimeMessage mimeMessage)
		{
			Error = error;
			FailureCounter = failureCounter;
			SmtpClientConfig = smtpClientConfig;
			MimeMessage = mimeMessage;
		}
	}

	/// <summary>
	/// Argument used by the event when getting the merged MimeMessage of the MailMergeMessage has failed.
	/// </summary>
	public class MailMessageFailureEventArgs : EventArgs
	{
		public readonly Exception Error;
		public readonly MailMergeMessage MailMergeMessage;
		public readonly object DataSource;

		internal MailMessageFailureEventArgs(Exception error, MailMergeMessage mailMergeMessage, object dataSource)
		{
			Error = error;
			MailMergeMessage = mailMergeMessage;
			DataSource = dataSource;
		}
	}
}