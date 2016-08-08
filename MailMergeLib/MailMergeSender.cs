using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MailKit;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;


namespace MailMergeLib
{
	/// <summary>
	/// Sends MailMergeMessages to an SMTP server. It uses System.Net.Mail.SmtpClient for low level operations.
	/// </summary>
	public class MailMergeSender : IDisposable
	{
		private SmtpClientConfig _smtpClientConfig;
		private int _maxNumOfSmtpClients = 5;
		private int _maxFailures = 1;
		private int _retryDelayTime;

		private bool _disposed;
		private bool _sendCancel;
		public readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();


		private MailMergeSender()
		{
			ReadyMerged = true;
			ReadySent = true;
			IsBusy = false;
		}

		/// <summary>
		/// MailMergeSender CTOR
		/// </summary>
		/// <param name="readDefaultsFromConfigFile">If true (default), default settings will be read from configuration system.net/mailSettings/smtp</param>
		public MailMergeSender(bool readDefaultsFromConfigFile = true) : this()
		{
			SmtpClientConfig = new SmtpClientConfig(readDefaultsFromConfigFile);
		}

		/// <summary>
		/// Gets or sets the maximum number of SmtpClient to send messages concurrently.
		/// Valid numbers are 1 to 20, defaults to 5.
		/// </summary>
		public int MaxNumOfSmtpClients
		{
			get { return _maxNumOfSmtpClients; }
			set { _maxNumOfSmtpClients = value > 0 && value <= 20 ? value : 5; }
		}

		/// <summary>
		/// Returns true, while a Send method is pending.
		/// Entering a Send method while IsBusy will raise an InvalidOperationException.
		/// </summary>
		public bool IsBusy { get; private set; }
		
		/// <summary>
		/// Returns true, if the transaction of sending a message has completed, else false.
		/// </summary>
		public bool ReadySent { get; private set; }

		/// <summary>
		/// Returns true, if the mail merge transaction has completed, else false.
		/// </summary>
		public bool ReadyMerged { get; private set; }

		/// <summary>
		/// Gets or sets the number of failures (1-5) for which a retry to send will be performed.
		/// </summary>
		public int MaxFailures
		{
			get { return _maxFailures; }
			set { _maxFailures = (value >= 1 && value < 5) ? value : 1; }
		}

		/// <summary>
		/// Gets or sets the delay time in milliseconds (0-10000) to elaps between retries to send the message.
		/// </summary>
		public int RetryDelayTime
		{
			get { return _retryDelayTime; }
			set { _retryDelayTime = (value >= 0 && value <= 10000) ? value : 0; }
		}

		/// <summary>
		/// Gets or sets the delay time in milliseconds (0-10000) between the messages.
		/// In case more than one SmtpClient will be used concurrently, the delay will be used per thread.
		/// Mainly used for debug purposes.
		/// </summary>
		public int DelayBetweenMessages { get; set; }


		/// <summary>
		/// Sends mail messages asynchronously to all recipients supplied in the data source
		/// of the mail merge message.
		/// </summary>
		/// <param name="mailMergeMessage">Mail merge message.</param>
		/// <param name="dataSource">IEnumerable data source with values for the placeholders of the MailMergeMessage.</param>
		/// <exception>
		/// If the SMTP transaction is the cause, SmtpFailedRecipientsException, SmtpFailedRecipientException or SmtpException can be expected.
		/// These exceptions throw after re-trying to send after failures (i.e. after MaxFailures * RetryDelayTime).
		/// </exception>
		/// <exception cref="InvalidOperationException">A send operation is pending.</exception>
		/// <exception cref="AggregateException"></exception>
		public async Task SendAsync<T>(MailMergeMessage mailMergeMessage, IEnumerable<T> dataSource)
		{
			if (mailMergeMessage == null || dataSource == null)
				throw new NullReferenceException($"{nameof(mailMergeMessage)} and {nameof(dataSource)} must not be null.");

			if (IsBusy)
				throw new InvalidOperationException($"{nameof(SendAsync)}: A send operation is pending in this instance of {nameof(MailMergeSender)}.");

			IsBusy = true;
			ReadyMerged = false;
			var sentMsgCount = 0;
			var errorMsgCount = 0;

			EventHandler<MailSenderAfterSendEventArgs> afterSend = (obj, args) =>
				                                                    {
				                                                       	if (args.Error == null)
				                                                       		Interlocked.Increment(ref sentMsgCount);
				                                                       	else
																			Interlocked.Increment(ref errorMsgCount);
				                                                    };
			OnAfterSend += afterSend;

			var startTime = DateTime.Now;
			
			var queue = new ConcurrentQueue<T>(dataSource);

			var numOfRecords = queue.Count;
			var sendTasks = new Task[_maxNumOfSmtpClients];

			for (var i = 0; i < sendTasks.Length; i++)
			{
				var taskNo = i;
				sendTasks[i] = Task.Run(() =>
				{
					using (var smtpClient = GetConfiguredSmtpClient())
					{
						T dataItem;
						while (queue.TryDequeue(out dataItem))
						{
							MimeMessage mimeMessage;
							try
							{
								mimeMessage = mailMergeMessage.GetMimeMessage(dataItem);
#if DEBUG
								if (mimeMessage.Headers[HeaderId.XMailer] != null) mimeMessage.Headers[HeaderId.XMailer] += "  #" + taskNo;
								else mimeMessage.Headers[HeaderId.XMailer] = "Task #" + taskNo;
#endif
							}
							catch (MailMergeMessage.MailMergeMessageException ex)
							{
								OnMessageFailure?.Invoke(this, new MailMessageFailureEventArgs(ex, mailMergeMessage, dataItem));
								return;
							}
							catch (Exception ex)
							{
								OnMessageFailure?.Invoke(this, new MailMessageFailureEventArgs(ex, mailMergeMessage, dataItem));
								return;
							}

							try
							{
								OnMergeProgress?.Invoke(this,
									new MailSenderMergeProgressEventArgs(startTime, numOfRecords, sentMsgCount, errorMsgCount, null, true));

								SendMimeMessage(smtpClient, mimeMessage); 

								OnMergeProgress?.Invoke(this,
									new MailSenderMergeProgressEventArgs(startTime, numOfRecords, sentMsgCount, errorMsgCount, null, true));
							}
							catch (Exception ex)
							{
								OnSendFailure?.Invoke(smtpClient, new MailSenderSendFailureEventArgs(ex, 1, 1, 0, mimeMessage));
							}
						}

						try
						{
							smtpClient.Disconnect(true);
						}
						catch (Exception)
						{
							// don't care for excpetion when disconnecting,
							// because smptClient will be disposed immediately anyway
						}

						Task.Delay(DelayBetweenMessages).Wait(_cancellationTokenSource.Token);
					}
				}, _cancellationTokenSource.Token);
			}

			try
			{
				OnMergeBegin?.Invoke(this, new MailSenderMergeBeginEventArgs(startTime, numOfRecords));

				//Task.WaitAll(sendTasks, _cancellationTokenSource.Token);
				await Task.WhenAll(sendTasks.AsEnumerable());

				OnMergeComplete?.Invoke(this,
					new MailSenderMergeCompleteEventArgs(_sendCancel, startTime, DateTime.Now, sentMsgCount));
			}
			catch (AggregateException exception)
			{
				foreach (var ex in exception.InnerExceptions)
				{
					OnSendFailure?.Invoke(this, new MailSenderSendFailureEventArgs(ex, 1, 1, RetryDelayTime, null));
				}
				throw;
			}
			finally
			{
				OnAfterSend -= afterSend;
				ReadyMerged = true;

				IsBusy = false;
			}
		}

		/// <summary>
		/// Sends a single mail message asyncronously.
		/// </summary>
		/// <remarks>The method raises events before and after sending, as well as on send failure.</remarks>
		/// <param name="mailMergeMessage">Mail merge message.</param>
		/// <param name="dataItem"></param>
		/// <exception>
		/// If the SMTP transaction is the cause, SmtpFailedRecipientsException, SmtpFailedRecipientException or SmtpException can be expected.
		/// These exceptions throw after re-trying to send after failures (i.e. after MaxFailures * RetryDelayTime).
		/// </exception>
		/// <exception cref="InvalidOperationException">A send operation is pending.</exception>
		/// <exception cref="AggregateException"></exception>
		public async Task SendAsync(MailMergeMessage mailMergeMessage, object dataItem)
		{
			if (IsBusy)
				throw new InvalidOperationException($"{nameof(SendAsync)}: A send operation is pending in this instance of {nameof(MailMergeSender)}.");

			IsBusy = true;

			try
			{
				await Task.Factory.StartNew(() =>
				{
					using (var smtpClient = GetConfiguredSmtpClient())
					{
						SendMimeMessage(smtpClient, mailMergeMessage.GetMimeMessage(dataItem));
					}

				}, _cancellationTokenSource.Token, TaskCreationOptions.None, TaskScheduler.Default);
			}
			finally
			{
				IsBusy = false;
			}
		}

		/// <summary>
		/// Sends mail messages syncronously to all recipients supplied in the data source
		/// of the mail merge message.
		/// </summary>
		/// <param name="mailMergeMessage">Mail merge message.</param>
		/// <param name="dataSource">IEnumerable data source with values for the placeholders of the MailMergeMessage.</param>
		/// <exception>
		/// If the SMTP transaction is the cause, SmtpFailedRecipientsException, SmtpFailedRecipientException or SmtpException can be expected.
		/// These exceptions throw after re-trying to send after failures (i.e. after MaxFailures * RetryDelayTime).
		/// </exception>
		/// <exception cref="InvalidOperationException">A send operation is pending.</exception>
		/// <exception cref="SmtpCommandException"></exception>
		/// <exception cref="SmtpProtocolException"></exception>
		/// <exception cref="AuthenticationException"></exception>
		public void Send<T>(MailMergeMessage mailMergeMessage, IEnumerable<T> dataSource)
		{
			if (IsBusy)
				throw new InvalidOperationException($"{nameof(Send)}: A send operation is pending in this instance of {nameof(MailMergeSender)}.");

			IsBusy = true;

			try
			{
				var startTime = DateTime.Now;
				var numOfRecords = dataSource.Count();
				var sentMsgCount = 0;
				var errorMsgCount = 0;

				using (var smtpClient = GetConfiguredSmtpClient())
				{
					OnMergeBegin?.Invoke(this, new MailSenderMergeBeginEventArgs(startTime, numOfRecords));

					foreach (var dataItem in dataSource)
					{
						OnMergeProgress?.Invoke(this, new MailSenderMergeProgressEventArgs(startTime, numOfRecords, sentMsgCount, errorMsgCount, null, true));

						var mimeMessage = mailMergeMessage.GetMimeMessage(dataItem);
						SendMimeMessage(smtpClient, mimeMessage);
						sentMsgCount++;

						OnMergeProgress?.Invoke(this, new MailSenderMergeProgressEventArgs(startTime, numOfRecords, sentMsgCount, errorMsgCount, null, true));
					}

					OnMergeComplete?.Invoke(this, new MailSenderMergeCompleteEventArgs(_sendCancel, startTime, DateTime.Now, sentMsgCount));
				}
			}
			finally
			{
				IsBusy = false;
			}
		}

		/// <summary>
		/// Sends a single mail merge message.
		/// </summary>
		/// <param name="mailMergeMessage">Message to send.</param>
		/// <param name="dataItem"></param>
		/// <exception>
		/// If the SMTP transaction is the cause, SmtpFailedRecipientsException, SmtpFailedRecipientException or SmtpException can be expected.
		/// These exceptions throw after re-trying to send after failures (i.e. after MaxFailures * RetryDelayTime).
		/// </exception>
		/// <exception cref="InvalidOperationException">A send operation is pending.</exception>
		/// <exception cref="SmtpCommandException"></exception>
		/// <exception cref="SmtpProtocolException"></exception>
		/// <exception cref="AuthenticationException"></exception>
		public void Send(MailMergeMessage mailMergeMessage, object dataItem)
		{
			if (IsBusy)
				throw new InvalidOperationException($"{nameof(Send)}: A send operation is pending in this instance of {nameof(MailMergeSender)}.");

			IsBusy = true;

			try
			{
				using (var smtpClient = GetConfiguredSmtpClient())
				{
					SendMimeMessage(smtpClient, mailMergeMessage.GetMimeMessage(dataItem));
				}
			}
			finally
			{
				IsBusy = false;
			}
		}

		/// <summary>
		/// This is the procedure taking care of sending the message (or saving to a file, respectively).
		/// </summary>
		/// <param name="smtpClient">The fully configures SmtpClient used to send the MimeMessate</param>
		/// <param name="mimeMsg">Mime message to send.</param>
		/// <exception>
		/// If the SMTP transaction is the cause, SmtpFailedRecipientsException, SmtpFailedRecipientException or SmtpException can be expected.
		/// These exceptions throw after re-trying to send after failures (i.e. after MaxFailures * RetryDelayTime).
		/// </exception>
		/// <exception cref="SmtpCommandException"></exception>
		/// <exception cref="SmtpProtocolException"></exception>
		/// <exception cref="AuthenticationException"></exception>
		private void SendMimeMessage(SmtpClient smtpClient, MimeMessage mimeMsg)
		{
			if (!ReadyMerged && !ReadySent)
				_sendCancel = false;

			var startTime = DateTime.Now;
			Exception sendException = null;

			ReadySent = false;

			// the client can rely on the sequence of events: OnBeforeSend, OnSendFailure (if any), OnAfterSend
			OnBeforeSend?.Invoke(smtpClient, new MailSenderBeforeSendEventArgs(null, _sendCancel, mimeMsg, startTime));

			var failureCounter = 0;
			do
			{
				try
				{
					sendException = null;
					switch (SmtpClientConfig.MessageOutput)
					{
						case MessageOutput.None:
							break;
						case MessageOutput.Directory:
							mimeMsg.WriteTo(System.IO.Path.Combine(SmtpClientConfig.MailOutputDirectory, Guid.NewGuid().ToString("N") + ".eml"));
							break;

						case MessageOutput.PickupDirectoryFromIis:
							mimeMsg.WriteTo(System.IO.Path.Combine(SmtpClientConfig.MailOutputDirectory, Guid.NewGuid().ToString("N")));
							break;
						default:
							SendMimeMessageToSmtpServer(smtpClient, mimeMsg);
							break; // break switch
					}
					// when SendMimeMessageToSmtpServer throws less than _maxFailures exceptions,
					// and succeeds after an exception, we MUST break the while loop here (else: infinite)
					break;
				}
				catch (Exception ex)
				{
					// exceptions which are thrown by SmtpClient:
					if (ex is SmtpCommandException || ex is SmtpProtocolException ||
					    ex is AuthenticationException)
					{
						failureCounter++;
						sendException = ex;
						OnSendFailure?.Invoke(smtpClient,
							new MailSenderSendFailureEventArgs(sendException, failureCounter, MaxFailures, RetryDelayTime,
								mimeMsg));
						Task.Delay(RetryDelayTime).Wait(_cancellationTokenSource.Token);
					}
					else
					{
						failureCounter = MaxFailures;
						sendException = ex;
						OnSendFailure?.Invoke(smtpClient, new MailSenderSendFailureEventArgs(sendException, 1, 1, 0, mimeMsg));
					}
				}
			} while (failureCounter < MaxFailures && failureCounter > 0);

			OnAfterSend?.Invoke(smtpClient,
				new MailSenderAfterSendEventArgs(sendException, _sendCancel, mimeMsg, startTime, DateTime.Now));

			ReadySent = true;

			// Do some clean-up with the message
			foreach (var mimeEntity in mimeMsg.Attachments)
			{
				var att = mimeEntity as MimePart;
				att?.ContentObject.Stream.Dispose();
			}
	
			if (sendException != null)
			{
				throw sendException;
			}
		}

		/// <summary>
		/// Sends the MimeMessage to an SMTP server. This is the lowest level of sending a message.
		/// Connects and authenficates if necessary, but leaves the connection open.
		/// </summary>
		/// <param name="smtpClient"></param>
		/// <param name="message"></param>
		private void SendMimeMessageToSmtpServer(SmtpClient smtpClient, MimeMessage message)
		{
			try
			{
				if (!smtpClient.IsConnected)
				{
					smtpClient.Connect(SmtpClientConfig.SmtpHost, SmtpClientConfig.SmtpPort, SmtpClientConfig.SecureSocketOptions,
						_cancellationTokenSource.Token);

				}
			}
			catch (SmtpCommandException ex)
			{
				throw new SmtpCommandException(ex.ErrorCode, ex.StatusCode, ex.Mailbox,
					$"Error trying to connect {SmtpClientConfig.SmtpHost}:{SmtpClientConfig.SmtpPort}. " + ex.Message);
			}
			catch (SmtpProtocolException ex)
			{
				throw new SmtpProtocolException(
					$"Error trying to connect {SmtpClientConfig.SmtpHost}:{SmtpClientConfig.SmtpPort}. " + ex.Message);
			}
			
			if (SmtpClientConfig.Credentials != null && !smtpClient.IsAuthenticated && smtpClient.Capabilities.HasFlag(SmtpCapabilities.Authentication))
			{
				try
				{
					smtpClient.Authenticate(SmtpClientConfig.Credentials, _cancellationTokenSource.Token);
				}
				catch (AuthenticationException ex)
				{
					throw new AuthenticationException($"Error trying to authenticate on {SmtpClientConfig.SmtpHost}:{SmtpClientConfig.SmtpPort}. " + ex.Message);
				}
				catch (SmtpCommandException ex)
				{
					throw new SmtpCommandException(ex.ErrorCode, ex.StatusCode, ex.Mailbox, $"Error trying to authenticate on {SmtpClientConfig.SmtpHost}:{SmtpClientConfig.SmtpPort}. " + ex.Message);
				}
				catch (SmtpProtocolException ex)
				{
					throw new SmtpProtocolException($"Error trying to authenticate on {SmtpClientConfig.SmtpHost}:{SmtpClientConfig.SmtpPort}. " + ex.Message);
				}
			}

			try
			{
				smtpClient.Send(message, _cancellationTokenSource.Token);
			}
			catch (SmtpCommandException ex)
			{
				switch (ex.ErrorCode)
				{
					case SmtpErrorCode.RecipientNotAccepted:
						throw new SmtpCommandException(ex.ErrorCode, ex.StatusCode, ex.Mailbox, $"Recipient not accepted by {SmtpClientConfig.SmtpHost}:{SmtpClientConfig.SmtpPort}. " + ex.Message);
					case SmtpErrorCode.SenderNotAccepted:
						throw new SmtpCommandException(ex.ErrorCode, ex.StatusCode, ex.Mailbox, $"Sender not accepted by {SmtpClientConfig.SmtpHost}:{SmtpClientConfig.SmtpPort}. " + ex.Message);
					case SmtpErrorCode.MessageNotAccepted:
						throw new SmtpCommandException(ex.ErrorCode, ex.StatusCode, ex.Mailbox, $"Message not accepted by {SmtpClientConfig.SmtpHost}:{SmtpClientConfig.SmtpPort}. " + ex.Message);
					default:
						throw new SmtpCommandException(ex.ErrorCode, ex.StatusCode, ex.Mailbox, $"Error sending message to {SmtpClientConfig.SmtpHost}:{SmtpClientConfig.SmtpPort}. " + ex.Message);
				}
			}
			catch (SmtpProtocolException ex)
			{
				throw new SmtpProtocolException($"Error while sending message to {SmtpClientConfig.SmtpHost}:{SmtpClientConfig.SmtpPort}. " + ex.Message);
			}
		}


		/// <summary>
		/// Get pre-configured SmtpClient using SmtpClientConfig
		/// </summary>
		private SmtpClient GetConfiguredSmtpClient()
		{
var smtpClient = new SmtpClient(new ProtocolLogger(@"C:\temp\mail\SmtpLog_" + System.IO.Path.GetRandomFileName() + ".txt"));
			//var smtpClient = SmtpClientConfig.ProtocolLogger != null ? new SmtpClient(SmtpClientConfig.ProtocolLogger) : new SmtpClient();
			smtpClient.Timeout = SmtpClientConfig.Timeout;
			smtpClient.LocalDomain = SmtpClientConfig.LocalHostName;
			// smtp.AuthenticationMechanisms.Remove("XOAUTH2");
			return smtpClient;
		}

		/// <summary>
		/// Gets or sets the configuration the SmtpClients will use.
		/// </summary>
		public SmtpClientConfig SmtpClientConfig
		{
			get { return _smtpClientConfig; }
			set { _smtpClientConfig = value ?? new SmtpClientConfig(false); }
		}

		/// <summary>
		/// Event raising before sending a mail message.
		/// </summary>
		public event EventHandler<MailMessageFailureEventArgs> OnMessageFailure;

		/// <summary>
		/// Event raising before sending a mail message.
		/// </summary>
		public event EventHandler<MailSenderBeforeSendEventArgs> OnBeforeSend;

		/// <summary>
		/// Event raising after sending a mail message.
		/// </summary>
		public event EventHandler<MailSenderAfterSendEventArgs> OnAfterSend;

		/// <summary>
		/// Event raising, if an error occurs when sending a mail message.
		/// </summary>
		public event EventHandler<MailSenderSendFailureEventArgs> OnSendFailure;

		/// <summary>
		/// Event raising before starting with mail merge.
		/// </summary>
		public event EventHandler<MailSenderMergeBeginEventArgs> OnMergeBegin;

		/// <summary>
		/// Event raising during mail merge progress, i.e. after each message sent.
		/// </summary>
		public event EventHandler<MailSenderMergeProgressEventArgs> OnMergeProgress;

		/// <summary>
		/// Event raising after completing mail merge.
		/// </summary>
		public event EventHandler<MailSenderMergeCompleteEventArgs> OnMergeComplete;

		/// <summary>
		/// Cancel any transactions sending or merging mail.
		/// </summary>
		public void SendCancel()
		{
			if (_cancellationTokenSource.IsCancellationRequested) return;

			_sendCancel = true;
		}

		/// <summary>
		/// Destructor.
		/// </summary>
		~MailMergeSender()
		{
			Dispose(false);
		}

		#region IDisposable Members

		/// <summary>
		/// Releases all resources used by MailMergeSender
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

		private void Dispose(bool disposing)
		{
			if (! _disposed)
			{
				if (disposing)
				{
					// Dispose managed resources.
				}

				// Clean up unmanaged resources here.
			}
			_disposed = true;
		}
	}
}