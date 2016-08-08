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
		public readonly DateTime EndTime;
		public readonly Exception Error;
		public readonly MimeMessage MimeMessage;
		public readonly DateTime StartTime;

		internal MailSenderAfterSendEventArgs(Exception error, bool cancelled, MimeMessage mailMergeMessage,
		                                      DateTime startTime, DateTime endTime)
		{
			Error = error;
			Cancelled = cancelled;
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
		public readonly MimeMessage MimeMessage;
		public readonly DateTime StartTime;

		internal MailSenderBeforeSendEventArgs(Exception error, bool cancelled, MimeMessage mimeMessage,
		                                       DateTime startTime)
		{
			Error = error;
			Cancelled = cancelled;
			MimeMessage = mimeMessage;
			StartTime = startTime;
		}
	}

	/// <summary>
	/// Argument used by the event before starting a mail merge.
	/// </summary>
	public class MailSenderMergeBeginEventArgs : EventArgs
	{
		public readonly DateTime StartTime;
		public readonly int NumOfMessagesToSend;

		internal MailSenderMergeBeginEventArgs(DateTime startTime, int numOfMessagesToSend)
		{
			StartTime = startTime;
			NumOfMessagesToSend = numOfMessagesToSend;
		}
	}

	/// <summary>
	/// Argument used by the event after every mail sent during a mail merge.
	/// </summary>
	public class MailSenderMergeProgressEventArgs : EventArgs
	{
		public readonly int ErrorMsg;
		public readonly MimeMessage MimeMessage;
		public readonly int SentMsg;
		public readonly DateTime StartTime;
		public readonly int TotalMsg;
		public bool Completed;

		internal MailSenderMergeProgressEventArgs(DateTime startTime, int totalMsg, int sentMsg, int errorMsg,
		                                          MimeMessage mimeMessage, bool completed)
		{
			StartTime = startTime;
			TotalMsg = totalMsg;
			SentMsg = sentMsg;
			ErrorMsg = errorMsg;
			MimeMessage = mimeMessage;
			Completed = completed;
		}
	}

	/// <summary>
	/// Argument used by the event after finishing a mail merge.
	/// </summary>
	public class MailSenderMergeCompleteEventArgs : EventArgs
	{
		public readonly bool Cancelled;
		public readonly DateTime EndTime;
		public readonly int MailsSent;
		public readonly DateTime StartTime;

		internal MailSenderMergeCompleteEventArgs(bool cancelled, DateTime startTime, DateTime endTime, int mailsSent)
		{
			Cancelled = cancelled;
			StartTime = startTime;
			EndTime = endTime;
			MailsSent = mailsSent;
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
		public readonly int MaxFailures;
		public readonly int RetryDelayTime;

		internal MailSenderSendFailureEventArgs(Exception error, int failureCounter, int maxFailures, int retryDelayTime,
		                                        MimeMessage mimeMessage)
		{
			Error = error;
			FailureCounter = failureCounter;
			MaxFailures = maxFailures;
			RetryDelayTime = retryDelayTime;
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