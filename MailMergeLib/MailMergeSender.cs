using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit.Cryptography;


namespace MailMergeLib
{
	/// <summary>
	/// Sends MailMergeMessages to an SMTP server. It uses MailKit.Net.Smtp.SmtpClient for low level operations.
	/// </summary>
	public class MailMergeSender : IDisposable
	{
		private int _maxNumOfSmtpClients = 5;

		private bool _disposed;
		private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

		/// <summary>
		/// CTOR
		/// </summary>
		public MailMergeSender()
		{
			IsBusy = false;
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
		/// Sends mail messages asynchronously to all recipients supplied in the data source
		/// of the mail merge message.
		/// </summary>
		/// <param name="mailMergeMessage">Mail merge message.</param>
		/// <param name="dataSource">IEnumerable data source with values for the placeholders of the MailMergeMessage.
		/// IEnumerable&lt;T&gt; where T can be the following types:
		/// Dictionary&lt;string,object&gt;, ExpandoObject, DataRow, class instances or anonymous types.
		/// The named placeholders can be the name of a Property, Field, or a parameterless method.
		/// They can also be chained together by using &quot;dot-notation&quot;.
		/// </param>
		/// <remarks>
		/// In order to use a DataTable as a dataSource, use System.Data.DataSetExtensions and convert it with DataTable.AsEnumerable()
		/// </remarks>
		/// <exception>
		/// If the SMTP transaction is the cause, SmtpFailedRecipientsException, SmtpFailedRecipientException or SmtpException can be expected.
		/// These exceptions throw after re-trying to send after failures (i.e. after MaxFailures * RetryDelayTime).
		/// </exception>
		/// <exception cref="InvalidOperationException">A send operation is pending.</exception>
		/// <exception cref="NullReferenceException"></exception>
		/// <exception cref="Exception">The first exception found in one of the async tasks.</exception>
		public async Task SendAsync<T>(MailMergeMessage mailMergeMessage, IEnumerable<T> dataSource)
		{
			if (mailMergeMessage == null || dataSource == null)
				throw new NullReferenceException($"{nameof(mailMergeMessage)} and {nameof(dataSource)} must not be null.");

			if (IsBusy)
				throw new InvalidOperationException($"{nameof(SendAsync)}: A send operation is pending in this instance of {nameof(MailMergeSender)}.");

			IsBusy = true;
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

			// The max. number of configurations used is the number of parallel smtp clients
			var smtpConfigForTask = new ISmtpClientConfig[_maxNumOfSmtpClients];
			// Set as many smtp configs as we have for each task
			// Example: 5 tasks with 2 configs: task 0 => config 0, task 1 => config 1, task 2 => config 0, task 3 => config 1, task 4 => config 0, task 5 => config 1
			for (var i = 0; i < _maxNumOfSmtpClients; i++)
			{
				smtpConfigForTask[i] = Config.SmtpClientConfig[i% Config.SmtpClientConfig.Length];
			}

			for (var i = 0; i < sendTasks.Length; i++)
			{
				var taskNo = i;
				sendTasks[i] = Task.Run(() =>
				{
					using (var smtpClient = GetInitializedSmtpClient(smtpConfigForTask[taskNo]))
					{
						T dataItem;
						while (queue.TryDequeue(out dataItem))
						{
							MimeMessage mimeMessage;
							try
							{
								mimeMessage = mailMergeMessage.GetMimeMessage(dataItem);
#if DEBUG
								mimeMessage.Headers[HeaderId.XMailer] = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name +"  #" + taskNo;
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

							OnMergeProgress?.Invoke(this,
								new MailSenderMergeProgressEventArgs(startTime, numOfRecords, sentMsgCount, errorMsgCount));

							SendMimeMessage(smtpClient, mimeMessage, smtpConfigForTask[taskNo]); 

							OnMergeProgress?.Invoke(this,
								new MailSenderMergeProgressEventArgs(startTime, numOfRecords, sentMsgCount, errorMsgCount));

							Task.Delay(smtpConfigForTask[taskNo].DelayBetweenMessages).Wait(_cancellationTokenSource.Token);
						}

						try
						{
							smtpClient.Disconnect(true);
						}
						catch (Exception)
						{
							// don't care for exception when disconnecting,
							// because smptClient will be disposed immediately anyway
						}
						smtpClient.ProtocolLogger?.Dispose();
					}
				}, _cancellationTokenSource.Token);
			}

			try
			{
				OnMergeBegin?.Invoke(this, new MailSenderMergeBeginEventArgs(startTime, numOfRecords));

				// Note await Task.WhenAll will only throw the FIRST exception of the aggregate exception!
				await Task.WhenAll(sendTasks.AsEnumerable());
			}
			finally
			{
				OnMergeComplete?.Invoke(this, new MailSenderMergeCompleteEventArgs(startTime, DateTime.Now, numOfRecords, sentMsgCount, errorMsgCount));

				OnAfterSend -= afterSend;

				IsBusy = false;
			}
		}

		/// <summary>
		/// Sends a single mail message asyncronously.
		/// </summary>
		/// <remarks>The method raises events before and after sending, as well as on send failure.</remarks>
		/// <param name="mailMergeMessage">Mail merge message.</param>
		/// <param name="dataItem">The following types are accepted:
		/// Dictionary&lt;string,object&gt;, ExpandoObject, DataRow, class instances or anonymous types.
		/// The named placeholders can be the name of a Property, Field, or a parameterless method.
		/// They can also be chained together by using &quot;dot-notation&quot;.
		/// </param>
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
				await Task.Run(() =>
				{
					var smtpClientConfig = Config.SmtpClientConfig[0]; // use the standard configuration
					using (var smtpClient = GetInitializedSmtpClient(smtpClientConfig))
					{
						SendMimeMessage(smtpClient, mailMergeMessage.GetMimeMessage(dataItem), smtpClientConfig);
						smtpClient.ProtocolLogger?.Dispose();
					}

				}, _cancellationTokenSource.Token);
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
		/// <param name="dataSource">IEnumerable data source with values for the placeholders of the MailMergeMessage.
		/// IEnumerable&lt;T&gt; where T can be the following types:
		/// Dictionary&lt;string,object&gt;, ExpandoObject, DataRow, class instances or anonymous types.
		/// The named placeholders can be the name of a Property, Field, or a parameterless method.
		/// They can also be chained together by using &quot;dot-notation&quot;.
		/// </param>
		/// <remarks>
		/// In order to use a DataTable as a dataSource, use System.Data.DataSetExtensions and convert it with DataTable.AsEnumerable()
		/// </remarks>
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

			var sentMsgCount = 0;

			try
			{
				var startTime = DateTime.Now;
				var numOfRecords = dataSource.Count();

				var smtpClientConfig = Config.SmtpClientConfig[0]; // use the standard configuration
				using (var smtpClient = GetInitializedSmtpClient(smtpClientConfig))
				{
					OnMergeBegin?.Invoke(this, new MailSenderMergeBeginEventArgs(startTime, numOfRecords));

					foreach (var dataItem in dataSource)
					{
						OnMergeProgress?.Invoke(this,
							new MailSenderMergeProgressEventArgs(startTime, numOfRecords, sentMsgCount, 0));

						var mimeMessage = mailMergeMessage.GetMimeMessage(dataItem);
						SendMimeMessage(smtpClient, mimeMessage, smtpClientConfig);
						sentMsgCount++;

						OnMergeProgress?.Invoke(this,
							new MailSenderMergeProgressEventArgs(startTime, numOfRecords, sentMsgCount, 0));
					}

					OnMergeComplete?.Invoke(this,
						new MailSenderMergeCompleteEventArgs(startTime, DateTime.Now, numOfRecords, sentMsgCount, 0));
					smtpClient.ProtocolLogger?.Dispose();
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
		/// <param name="dataItem">The following types are accepted:
		/// Dictionary&lt;string,object&gt;, ExpandoObject, DataRow, class instances or anonymous types.
		/// The named placeholders can be the name of a Property, Field, or a parameterless method.
		/// They can also be chained together by using &quot;dot-notation&quot;.
		/// </param>
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
				var smtpClientConfig = Config.SmtpClientConfig[0]; // use the standard configuration
				using (var smtpClient = GetInitializedSmtpClient(smtpClientConfig))
				{
					SendMimeMessage(smtpClient, mailMergeMessage.GetMimeMessage(dataItem), smtpClientConfig);
					smtpClient.ProtocolLogger?.Dispose();
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
		/// <param name="config"></param>
		/// <exception>
		/// If the SMTP transaction is the cause, SmtpFailedRecipientsException, SmtpFailedRecipientException or SmtpException can be expected.
		/// These exceptions throw after re-trying to send after failures (i.e. after MaxFailures * RetryDelayTime).
		/// </exception>
		/// <exception cref="SmtpCommandException"></exception>
		/// <exception cref="SmtpProtocolException"></exception>
		/// <exception cref="AuthenticationException"></exception>
		/// <exception cref="System.Net.Sockets.SocketException"></exception>
		private void SendMimeMessage(SmtpClient smtpClient, MimeMessage mimeMsg, ISmtpClientConfig config)
		{
			var startTime = DateTime.Now;
			Exception sendException = null;

			// the client can rely on the sequence of events: OnBeforeSend, OnSendFailure (if any), OnAfterSend
			OnBeforeSend?.Invoke(smtpClient, new MailSenderBeforeSendEventArgs(null, _cancellationTokenSource.Token.IsCancellationRequested, mimeMsg, startTime));

			var failureCounter = 0;
			do
			{
				try
				{
					sendException = null;
					const string mailExt = ".eml";
					switch (config.MessageOutput)
					{
						case MessageOutput.None:
							break;
						case MessageOutput.Directory:
							mimeMsg.WriteTo(System.IO.Path.Combine(config.MailOutputDirectory, Guid.NewGuid().ToString("N") + mailExt));
							break;
						case MessageOutput.PickupDirectoryFromIis:
							// for requirements of message format see: https://technet.microsoft.com/en-us/library/bb124230(v=exchg.150).aspx
							// and here http://www.vsysad.com/2014/01/iis-smtp-folders-and-domains-explained/
							mimeMsg.WriteTo(System.IO.Path.Combine(config.MailOutputDirectory, Guid.NewGuid().ToString("N") + mailExt));
							break;
						default:
							SendMimeMessageToSmtpServer(smtpClient, mimeMsg, config);
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
					    ex is AuthenticationException || ex is System.Net.Sockets.SocketException)
					{
						failureCounter++;
						sendException = ex;
						OnSendFailure?.Invoke(smtpClient,
							new MailSenderSendFailureEventArgs(sendException, failureCounter, config, mimeMsg));
						Task.Delay(config.RetryDelayTime).Wait(_cancellationTokenSource.Token);

						// on first SMTP failure switch to the backup configuration, if one exists
						if (failureCounter == 1 && config.MaxFailures > 1)
						{
							var backupConfig = Config.SmtpClientConfig.FirstOrDefault(c => c != config);
							if (backupConfig == null) continue;

							backupConfig.MaxFailures = config.MaxFailures; // keep the logic within the current loop unchanged
							SetConfigForSmtpClient(smtpClient, backupConfig);
							config = backupConfig;
						}
					}
					else
					{
						failureCounter = config.MaxFailures;
						sendException = ex;
						OnSendFailure?.Invoke(smtpClient, new MailSenderSendFailureEventArgs(sendException, 1, config, mimeMsg));
					}
				}
			} while (failureCounter < config.MaxFailures && failureCounter > 0);

			OnAfterSend?.Invoke(smtpClient,
				new MailSenderAfterSendEventArgs(sendException, _cancellationTokenSource.Token.IsCancellationRequested, mimeMsg, startTime, DateTime.Now));

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
		/// <param name="config"></param>
		private void SendMimeMessageToSmtpServer(SmtpClient smtpClient, MimeMessage message, ISmtpClientConfig config)
		{
			var hostPortConfig = $"{config.SmtpHost}:{config.SmtpPort} using configuration '{config.Name}'";
			const string errorConnect = "Error trying to connect";
			const string errorAuth = "Error trying to authenticate on";

			try
			{
				if (!smtpClient.IsConnected)
				{
					smtpClient.Connect(config.SmtpHost, config.SmtpPort, config.SecureSocketOptions,
						_cancellationTokenSource.Token);

				}
			}
			catch (SmtpCommandException ex)
			{
				throw new SmtpCommandException(ex.ErrorCode, ex.StatusCode, ex.Mailbox,
					$"{errorConnect} {hostPortConfig}'. " + ex.Message);
			}
			catch (SmtpProtocolException ex)
			{
				throw new SmtpProtocolException(
					$"{errorConnect} {hostPortConfig}'. " + ex.Message);
			}

			if (config.NetworkCredential != null && !smtpClient.IsAuthenticated && smtpClient.Capabilities.HasFlag(SmtpCapabilities.Authentication))
			{
				try
				{
					smtpClient.Authenticate(config.NetworkCredential, _cancellationTokenSource.Token);
				}
				catch (AuthenticationException ex)
				{
					throw new AuthenticationException($"{errorAuth} {hostPortConfig}. " + ex.Message);
				}
				catch (SmtpCommandException ex)
				{
					throw new SmtpCommandException(ex.ErrorCode, ex.StatusCode, ex.Mailbox, $"{errorAuth} {hostPortConfig}. " + ex.Message);
				}
				catch (SmtpProtocolException ex)
				{
					throw new SmtpProtocolException($"{errorAuth} {hostPortConfig}. " + ex.Message);
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
						throw new SmtpCommandException(ex.ErrorCode, ex.StatusCode, ex.Mailbox,
							$"Recipient not accepted by {hostPortConfig}. " + ex.Message);
					case SmtpErrorCode.SenderNotAccepted:
						throw new SmtpCommandException(ex.ErrorCode, ex.StatusCode, ex.Mailbox,
							$"Sender not accepted by {hostPortConfig}. " + ex.Message);
					case SmtpErrorCode.MessageNotAccepted:
						throw new SmtpCommandException(ex.ErrorCode, ex.StatusCode, ex.Mailbox,
							$"Message not accepted by {hostPortConfig}. " + ex.Message);
					default:
						throw new SmtpCommandException(ex.ErrorCode, ex.StatusCode, ex.Mailbox,
							$"Error sending message to {hostPortConfig}. " + ex.Message);
				}
			}
			catch (SmtpProtocolException ex)
			{
				throw new SmtpProtocolException($"Error while sending message to {hostPortConfig}. " + ex.Message, ex);
			}
		}


		/// <summary>
		/// Get pre-configured SmtpClient
		/// </summary>
		private static SmtpClient GetInitializedSmtpClient(ISmtpClientConfig config)
		{
			//var smtpClient = new SmtpClient(new ProtocolLogger(@"C:\temp\mail\SmtpLog_" + System.IO.Path.GetRandomFileName() + ".txt"));
			var smtpClient = config.EnableLogOutput ? new SmtpClient(config.GetProtocolLogger()) : new SmtpClient();
			SetConfigForSmtpClient(smtpClient, config);

			// smtpClient.AuthenticationMechanisms.Remove("XOAUTH2");
			return smtpClient;
		}

		/// <summary>
		/// Disconnects the SmtpClient if connected, and sets the new configuration.
		/// </summary>
		/// <remarks>
		/// Note: 
		/// Part of configuration will only be used by SmtpClient during Connect() or Authorize().
		/// Protocol logger settings do not change.
		/// </remarks>
		/// <param name="smtpClient"></param>
		/// <param name="config"></param>
		private static void SetConfigForSmtpClient(SmtpClient smtpClient, ISmtpClientConfig config)
		{
			try
			{	
				if (smtpClient.IsConnected)
					smtpClient.Disconnect(false);
			}
			catch (Exception)
			{}
			
			smtpClient.Timeout = config.Timeout;
			smtpClient.LocalDomain = config.ClientDomain;
			smtpClient.LocalEndPoint = config.LocalEndPoint;
			smtpClient.ServerCertificateValidationCallback = config.ServerCertificateValidationCallback;
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
		/// The settings for a MailMergeSender.
		/// </summary>
		public SenderConfig Config { get; set; }

		/// <summary>
		/// Cancel any transactions sending or merging mail.
		/// </summary>
		/// <param name="waitTime">The number of milliseconds to wait before cancelation.</param>
		public void SendCancel(int waitTime = 0)
		{
			if (_cancellationTokenSource.IsCancellationRequested) return;

			if (waitTime == 0) _cancellationTokenSource.Cancel();
			else _cancellationTokenSource.CancelAfter(new TimeSpan(0, 0, 0, 0, waitTime));
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