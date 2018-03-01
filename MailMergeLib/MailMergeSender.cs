using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace MailMergeLib
{
    /// <summary>
    /// Sends MailMergeMessages to an SMTP server. It uses MailKit.Net.Smtp.SmtpClient for low level operations.
    /// </summary>
    public class MailMergeSender : IDisposable
    {
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
        /// Returns true, while a Send method is pending.
        /// Entering a Send method while IsBusy will raise an InvalidOperationException.
        /// </summary>
        public bool IsBusy { get; private set; }
        
        #region *** Async Methods ***

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
        /// <exception cref="MailMergeMessage.MailMergeMessageException"></exception>
        public async Task SendAsync<T>(MailMergeMessage mailMergeMessage, IEnumerable<T> dataSource)
        {
            if (mailMergeMessage == null || dataSource == null)
                throw new NullReferenceException($"{nameof(mailMergeMessage)} and {nameof(dataSource)} must not be null.");

            if (IsBusy)
                throw new InvalidOperationException($"{nameof(SendAsync)}: A send operation is pending in this instance of {nameof(MailMergeSender)}.");

            IsBusy = true;
            var sentMsgCount = 0;
            var errorMsgCount = 0;

            var tasksUsed = new HashSet<int>();

            void AfterSend(object obj, MailSenderAfterSendEventArgs args)
            {
                if (args.Error == null)
                    Interlocked.Increment(ref sentMsgCount);
                else
                    Interlocked.Increment(ref errorMsgCount);
            }
            OnAfterSend += AfterSend;

            var startTime = DateTime.Now;
            
            var queue = new ConcurrentQueue<T>(dataSource);

            var numOfRecords = queue.Count;
            var sendTasks = new Task[Config.MaxNumOfSmtpClients];

            // The max. number of configurations used is the number of parallel smtp clients
            var smtpConfigForTask = new SmtpClientConfig[Config.MaxNumOfSmtpClients];
            // Set as many smtp configs as we have for each task
            // Example: 5 tasks with 2 configs: task 0 => config 0, task 1 => config 1, task 2 => config 0, task 3 => config 1, task 4 => config 0, task 5 => config 1
            for (var i = 0; i < Config.MaxNumOfSmtpClients; i++)
            {
                smtpConfigForTask[i] = Config.SmtpClientConfig[i% Config.SmtpClientConfig.Length];
            }

            for (var i = 0; i < sendTasks.Length; i++)
            {
                var taskNo = i;
#if NET40
                sendTasks[taskNo] = TaskEx.Run(async () =>
#else
                sendTasks[taskNo] = Task.Run(async () =>
#endif
                {
                    using (var smtpClient = GetInitializedSmtpClient(smtpConfigForTask[taskNo]))
                    {
                        while (queue.TryDequeue(out var dataItem))
                        {
                            lock (tasksUsed)
                            {
                                tasksUsed.Add(taskNo);
                            }

                            var localDataItem = dataItem;  // no modified enclosure
                            MimeMessage mimeMessage = null;
                            try
                            {
#if NET40
                                mimeMessage = await TaskEx.Run(() => mailMergeMessage.GetMimeMessage(localDataItem)).ConfigureAwait(false);
#else
                                mimeMessage = await Task.Run(() => mailMergeMessage.GetMimeMessage(localDataItem)).ConfigureAwait(false);
#endif
                            }
                            catch (Exception exception)
                            {
                                OnMergeProgress?.Invoke(this,
                                    new MailSenderMergeProgressEventArgs(startTime, numOfRecords, sentMsgCount, errorMsgCount));

                                var mmFailureEventArgs = new MailMessageFailureEventArgs(exception, mailMergeMessage, dataItem, mimeMessage, true);
                                if (exception is MailMergeMessage.MailMergeMessageException ex)
                                {
                                    mmFailureEventArgs = new MailMessageFailureEventArgs(ex, mailMergeMessage, dataItem,
                                        ex.MimeMessage, true);
                                }

                                OnMessageFailure?.Invoke(this, mmFailureEventArgs);
                                
                                // event delegate may have modified the mimeMessage and decided not to throw an exception
                                if (mmFailureEventArgs.ThrowException || mmFailureEventArgs.MimeMessage == null)
                                {
                                    Interlocked.Increment(ref errorMsgCount);

                                    // Fire promised events
                                    OnMergeProgress?.Invoke(this,
                                        new MailSenderMergeProgressEventArgs(startTime, numOfRecords, sentMsgCount,
                                            errorMsgCount));

                                    return;
                                }

                                // set MimeMessage from OnMessageFailure delegate
                                mimeMessage = mmFailureEventArgs.MimeMessage;
                            }

                            OnMergeProgress?.Invoke(this,
                                new MailSenderMergeProgressEventArgs(startTime, numOfRecords, sentMsgCount, errorMsgCount));
                            /*
                            if (OnMergeProgress != null)
                                Task.Factory.FromAsync((asyncCallback, obj) => OnMergeProgress.BeginInvoke(this, new MailSenderMergeProgressEventArgs(startTime, numOfRecords, sentMsgCount, errorMsgCount), asyncCallback, obj), OnMergeProgress.EndInvoke, null);
                            */
                            await SendMimeMessageAsync(smtpClient, mimeMessage, smtpConfigForTask[taskNo]).ConfigureAwait(false); 

                            OnMergeProgress?.Invoke(this,
                                new MailSenderMergeProgressEventArgs(startTime, numOfRecords, sentMsgCount, errorMsgCount));
#if NET40
                            await TaskEx.Delay(smtpConfigForTask[taskNo].DelayBetweenMessages, _cancellationTokenSource.Token).ConfigureAwait(false);
#else
                            await Task.Delay(smtpConfigForTask[taskNo].DelayBetweenMessages, _cancellationTokenSource.Token).ConfigureAwait(false);
#endif
                        }

                        smtpClient.ProtocolLogger?.Dispose();
                    }
                }, _cancellationTokenSource.Token);
            }

            try
            {
                OnMergeBegin?.Invoke(this, new MailSenderMergeBeginEventArgs(startTime, numOfRecords));

                // Note await Task.WhenAll will only throw the FIRST exception of the aggregate exception!
#if NET40
                await TaskEx.WhenAll(sendTasks.AsEnumerable()).ConfigureAwait(false);
#else
                await Task.WhenAll(sendTasks.AsEnumerable()).ConfigureAwait(false);
#endif

            }
            finally
            {
                OnMergeComplete?.Invoke(this, new MailSenderMergeCompleteEventArgs(startTime, DateTime.Now, numOfRecords, sentMsgCount, errorMsgCount, tasksUsed.Count));

                OnAfterSend -= AfterSend;

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
        /// <exception cref="MailMergeMessage.MailMergeMessageException"></exception>
        public async Task SendAsync(MailMergeMessage mailMergeMessage, object dataItem)
        {
            if (IsBusy)
                throw new InvalidOperationException($"{nameof(SendAsync)}: A send operation is pending in this instance of {nameof(MailMergeSender)}.");

            IsBusy = true;

            try
            {
#if NET40
                await TaskEx.Run(async () =>
#else
                await Task.Run(async () =>
#endif
                {
                    var smtpClientConfig = Config.SmtpClientConfig[0]; // use the standard configuration
                    using (var smtpClient = GetInitializedSmtpClient(smtpClientConfig))
                    {
                        MimeMessage mimeMessage = null;
                        try
                        {
                            mimeMessage = mailMergeMessage.GetMimeMessage(dataItem);
                        }
                        catch (Exception exception)
                        {
                            var mmFailureEventArgs = new MailMessageFailureEventArgs(exception, mailMergeMessage, dataItem, mimeMessage, true);
                            if (exception is MailMergeMessage.MailMergeMessageException ex)
                            {
                                mmFailureEventArgs = new MailMessageFailureEventArgs(ex, mailMergeMessage, dataItem,
                                    ex.MimeMessage, true);
                            }

                            OnMessageFailure?.Invoke(this, mmFailureEventArgs);

                            // event delegate may have modified the mimeMessage and decided not to throw an exception
                            if (mmFailureEventArgs.ThrowException || mmFailureEventArgs.MimeMessage == null)
                            {
                                throw;
                            }

                            // set MimeMessage from OnMessageFailure delegate
                            mimeMessage = mmFailureEventArgs.MimeMessage;
                        }

                        await SendMimeMessageAsync(smtpClient, mimeMessage, smtpClientConfig).ConfigureAwait(false);
                        smtpClient.ProtocolLogger?.Dispose();
                    }

                }, _cancellationTokenSource.Token).ConfigureAwait(false);
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
        private async Task SendMimeMessageAsync(SmtpClient smtpClient, MimeMessage mimeMsg, SmtpClientConfig config)
        {
            var startTime = DateTime.Now;
            Exception sendException;

            // the client can rely on the sequence of events: OnBeforeSend, OnSendFailure (if any), OnAfterSend
            OnBeforeSend?.Invoke(smtpClient, new MailSenderBeforeSendEventArgs(config, mimeMsg, startTime, null, _cancellationTokenSource.Token.IsCancellationRequested));

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
                            mimeMsg.WriteTo(System.IO.Path.Combine(config.MailOutputDirectory, Guid.NewGuid().ToString("N") + mailExt), _cancellationTokenSource.Token);
                            break;
#if NET40 || NET45
                        case MessageOutput.PickupDirectoryFromIis:
                            // for requirements of message format see: https://technet.microsoft.com/en-us/library/bb124230(v=exchg.150).aspx
                            // and here http://www.vsysad.com/2014/01/iis-smtp-folders-and-domains-explained/
                            mimeMsg.WriteTo(System.IO.Path.Combine(config.MailOutputDirectory, Guid.NewGuid().ToString("N") + mailExt), _cancellationTokenSource.Token);
                            break;
#endif
                        default:
                            await SendMimeMessageToSmtpServerAsync(smtpClient, mimeMsg, config).ConfigureAwait(false);
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
#if NET40
                        await TaskEx.Delay(config.RetryDelayTime, _cancellationTokenSource.Token).ConfigureAwait(false);
#else
                        await Task.Delay(config.RetryDelayTime, _cancellationTokenSource.Token).ConfigureAwait(false);
#endif
                        // on first SMTP failure switch to the backup configuration, if one exists
                        if (failureCounter == 1 && config.MaxFailures > 1)
                        {
                            var backupConfig = Config.SmtpClientConfig.FirstOrDefault(c => c != config);
                            if (backupConfig == null) continue;

                            backupConfig.MaxFailures = config.MaxFailures; // keep the logic within the current loop unchanged
                            config = backupConfig;
                            smtpClient = GetInitializedSmtpClient(config);
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
                new MailSenderAfterSendEventArgs(config, mimeMsg, startTime, DateTime.Now, sendException, _cancellationTokenSource.Token.IsCancellationRequested));

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
        private async Task SendMimeMessageToSmtpServerAsync(SmtpClient smtpClient, MimeMessage message, SmtpClientConfig config)
        {
            var hostPortConfig = $"{config.SmtpHost}:{config.SmtpPort} using configuration '{config.Name}'";
            const string errorConnect = "Error trying to connect";
            const string errorAuth = "Error trying to authenticate on";

            try
            {
                if (!smtpClient.IsConnected)
                {
                    await smtpClient.ConnectAsync(config.SmtpHost, config.SmtpPort, config.SecureSocketOptions,
                        _cancellationTokenSource.Token).ConfigureAwait(false);

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
                    await smtpClient.AuthenticateAsync(config.NetworkCredential, _cancellationTokenSource.Token).ConfigureAwait(false);
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
                await smtpClient.SendAsync(message, _cancellationTokenSource.Token).ConfigureAwait(false);
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
        /// Cancel any transactions sending or merging mail.
        /// </summary>
        /// <param name="waitTime">The number of milliseconds to wait before cancelation.</param>
        public void SendCancel(int waitTime = 0)
        {
            if (_cancellationTokenSource.IsCancellationRequested) return;

            if (waitTime == 0) _cancellationTokenSource.Cancel();
            else _cancellationTokenSource.CancelAfter(new TimeSpan(0, 0, 0, 0, waitTime));
        }

        #endregion

        #region *** Sync Methods ***

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
        /// <exception cref="MailMergeMessage.MailMergeMessageException"></exception>
        public void Send<T>(MailMergeMessage mailMergeMessage, IEnumerable<T> dataSource)
        {
            if (IsBusy)
                throw new InvalidOperationException($"{nameof(Send)}: A send operation is pending in this instance of {nameof(MailMergeSender)}.");

            IsBusy = true;
            
            var sentMsgCount = 0;

            try
            {
                var dataSourceList = dataSource.ToList();

                var startTime = DateTime.Now;
                var numOfRecords = dataSourceList.Count;

                var smtpClientConfig = Config.SmtpClientConfig[0]; // use the standard configuration
                using (var smtpClient = GetInitializedSmtpClient(smtpClientConfig))
                {
                    OnMergeBegin?.Invoke(this, new MailSenderMergeBeginEventArgs(startTime, numOfRecords));

                    foreach (var dataItem in dataSourceList)
                    {
                        OnMergeProgress?.Invoke(this,
                            new MailSenderMergeProgressEventArgs(startTime, numOfRecords, sentMsgCount, 0));

                        MimeMessage mimeMessage = null;
                        try
                        {
                            mimeMessage = mailMergeMessage.GetMimeMessage(dataItem);
                        }
                        catch (Exception exception)
                        {
                            var mmFailureEventArgs = new MailMessageFailureEventArgs(exception, mailMergeMessage, dataItem, mimeMessage, true);
                            if (exception is MailMergeMessage.MailMergeMessageException ex)
                            {
                                mmFailureEventArgs = new MailMessageFailureEventArgs(ex, mailMergeMessage, dataItem,
                                    ex.MimeMessage, true);
                            }

                            OnMessageFailure?.Invoke(this, mmFailureEventArgs);
                            
                            // event delegate may have modified the mimeMessage and decided not to throw an exception
                            if (mmFailureEventArgs.ThrowException || mmFailureEventArgs.MimeMessage == null)
                            {
                                // Invoke promised events
                                OnMergeProgress?.Invoke(this,
                                    new MailSenderMergeProgressEventArgs(startTime, numOfRecords, sentMsgCount, 1));
                                smtpClient.Dispose();
                                OnMergeComplete?.Invoke(this,
                                    new MailSenderMergeCompleteEventArgs(startTime, DateTime.Now, numOfRecords,
                                        sentMsgCount, 1, 1));
                                throw;
                            }

                            // set MimeMessage from OnMessageFailure delegate
                            mimeMessage = mmFailureEventArgs.MimeMessage;
                        }

                        if (_cancellationTokenSource.IsCancellationRequested) break;
                        SendMimeMessage(smtpClient, mimeMessage, smtpClientConfig);
                        sentMsgCount++;
                        if (_cancellationTokenSource.IsCancellationRequested) break;

                        OnMergeProgress?.Invoke(this,
                            new MailSenderMergeProgressEventArgs(startTime, numOfRecords, sentMsgCount, 0));

                        Thread.Sleep(smtpClientConfig.DelayBetweenMessages);
                        if (_cancellationTokenSource.IsCancellationRequested) break;
                    }
                    
                    smtpClient.ProtocolLogger?.Dispose();
                    smtpClient.Dispose(); // fire OnSmtpDisconnected before OnMergeComplete
                    OnMergeComplete?.Invoke(this,
                        new MailSenderMergeCompleteEventArgs(startTime, DateTime.Now, numOfRecords, sentMsgCount, 0, 1));
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
        /// <exception cref="MailMergeMessage.MailMergeMessageException"></exception>
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
                    MimeMessage mimeMessage = null;
                    try
                    {
                        mimeMessage = mailMergeMessage.GetMimeMessage(dataItem);
                    }
                    catch (Exception exception)
                    {
                        var mmFailureEventArgs = new MailMessageFailureEventArgs(exception, mailMergeMessage, dataItem, mimeMessage, true);
                        if (exception is MailMergeMessage.MailMergeMessageException ex)
                        {
                            mmFailureEventArgs = new MailMessageFailureEventArgs(ex, mailMergeMessage, dataItem,
                                ex.MimeMessage, true);
                        }

                        OnMessageFailure?.Invoke(this, mmFailureEventArgs);

                        // event delegate may have modified the mimeMessage and decided not to throw an exception
                        if (mmFailureEventArgs.ThrowException || mmFailureEventArgs.MimeMessage == null)
                        {
                            throw;
                        }
                        
                        // set MimeMessage from OnMessageFailure delegate
                        mimeMessage = mmFailureEventArgs.MimeMessage;
                    }

                    SendMimeMessage(smtpClient, mimeMessage, smtpClientConfig);
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
        private void SendMimeMessage(SmtpClient smtpClient, MimeMessage mimeMsg, SmtpClientConfig config)
        {
            var startTime = DateTime.Now;
            Exception sendException;

            // the client can rely on the sequence of events: OnBeforeSend, OnSendFailure (if any), OnAfterSend
            OnBeforeSend?.Invoke(smtpClient, new MailSenderBeforeSendEventArgs(config, mimeMsg, startTime, null, _cancellationTokenSource.Token.IsCancellationRequested));

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
                            mimeMsg.WriteTo(System.IO.Path.Combine(config.MailOutputDirectory, Guid.NewGuid().ToString("N") + mailExt), _cancellationTokenSource.Token);
                            break;
#if NET40 || NET45
                        case MessageOutput.PickupDirectoryFromIis:
                            // for requirements of message format see: https://technet.microsoft.com/en-us/library/bb124230(v=exchg.150).aspx
                            // and here http://www.vsysad.com/2014/01/iis-smtp-folders-and-domains-explained/
                            mimeMsg.WriteTo(System.IO.Path.Combine(config.MailOutputDirectory, Guid.NewGuid().ToString("N") + mailExt), _cancellationTokenSource.Token);
                            break;
#endif
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
                    sendException = ex;
                    // exceptions which are thrown by SmtpClient:
                    if (ex is SmtpCommandException || ex is SmtpProtocolException ||
                        ex is AuthenticationException || ex is System.Net.Sockets.SocketException)
                    {
                        failureCounter++;
                        OnSendFailure?.Invoke(smtpClient,
                            new MailSenderSendFailureEventArgs(sendException, failureCounter, config, mimeMsg));

                        Thread.Sleep(config.RetryDelayTime);

                        // on first SMTP failure switch to the backup configuration, if one exists
                        if (failureCounter == 1 && config.MaxFailures > 1)
                        {
                            var backupConfig = Config.SmtpClientConfig.FirstOrDefault(c => c != config);
                            if (backupConfig == null) continue;

                            backupConfig.MaxFailures = config.MaxFailures; // keep the logic within the current loop unchanged
                            config = backupConfig;
                            smtpClient = GetInitializedSmtpClient(config);
                        }
                    }
                    else
                    {
                        failureCounter = config.MaxFailures;
                        OnSendFailure?.Invoke(smtpClient, new MailSenderSendFailureEventArgs(sendException, 1, config, mimeMsg));
                    }
                }
            } while (failureCounter < config.MaxFailures && failureCounter > 0);

            OnAfterSend?.Invoke(smtpClient,
                new MailSenderAfterSendEventArgs(config, mimeMsg, startTime, DateTime.Now, sendException, _cancellationTokenSource.Token.IsCancellationRequested));

            // Dispose the streams of file attachments and inline file attachments
            MailMergeMessage.DisposeFileStreams(mimeMsg);

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
        private void SendMimeMessageToSmtpServer(SmtpClient smtpClient, MimeMessage message, SmtpClientConfig config)
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

        #endregion

        #region *** Events ***

        /// <summary>
        /// Event raising when getting the merged MimeMessage of the MailMergeMessage has failed.
        /// </summary>
        public event EventHandler<MailMessageFailureEventArgs> OnMessageFailure;

        /// <summary>
        /// Event raising before sending a mail message.
        /// </summary>
        public event EventHandler<MailSenderBeforeSendEventArgs> OnBeforeSend;

        /// <summary>
        /// Event raising right after the <see cref="SmtpClient"/>'s connection to the server is up (but not yet authenticated).
        /// </summary>
        public event EventHandler<MailSenderSmtpClientEventArgs> OnSmtpConnected;

        /// <summary>
        /// Event raising after the <see cref="SmtpClient"/> has authenticated on the server.
        /// </summary>
        public event EventHandler<MailSenderSmtpClientEventArgs> OnSmtpAuthenticated;

        /// <summary>
        /// Event raising after the <see cref="SmtpClient"/> has disconnected from the server.
        /// </summary>
        public event EventHandler<MailSenderSmtpClientEventArgs> OnSmtpDisconnected;

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
        public SenderConfig Config { get; set; } = new SenderConfig();

        #endregion

        /// <summary>
        /// Get a new instance of a pre-configured SmtpClient
        /// </summary>
        private SmtpClient GetInitializedSmtpClient(SmtpClientConfig config)
        {
            var smtpClient = config.ProtocolLoggerDelegate != null ? new SmtpClient(config.ProtocolLoggerDelegate?.Invoke()) : new SmtpClient();

            smtpClient.Timeout = config.Timeout;
            smtpClient.LocalDomain = config.ClientDomain;
            smtpClient.LocalEndPoint = config.LocalEndPoint;
            smtpClient.ClientCertificates = config.ClientCertificates;
            smtpClient.ServerCertificateValidationCallback = config.ServerCertificateValidationCallback;
            smtpClient.SslProtocols = config.SslProtocols;

            // redirect SmtpClient events
            smtpClient.Connected += (sender, args) => { OnSmtpConnected?.Invoke(smtpClient, new MailSenderSmtpClientEventArgs(config)); };
            smtpClient.Authenticated += (sender, args) => { OnSmtpAuthenticated?.Invoke(smtpClient, new MailSenderSmtpClientEventArgs(config)); };
            smtpClient.Disconnected += (sender, args) => { OnSmtpDisconnected?.Invoke(smtpClient, new MailSenderSmtpClientEventArgs(config)); };
            return smtpClient;
        }

        /// <summary>
        /// Destructor.
        /// </summary>
        ~MailMergeSender()
        {
            Dispose(false);
        }

        #region *** IDisposable Members ***

        /// <summary>
        /// Releases all resources used by MailMergeSender
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

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

#endregion

    }
}