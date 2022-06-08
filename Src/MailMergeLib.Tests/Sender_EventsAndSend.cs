using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MailKit.Security;
using MimeKit;
using netDumbster.smtp;
using NUnit.Framework;
using SmartFormat.Core.Settings;

namespace MailMergeLib.Tests;

[TestFixture]
public class Sender_EventsAndSend
{
    private static readonly object _locker = new();
    private static SimpleSmtpServer? _server;
    private Settings _settings = new();

    public Sender_EventsAndSend()
    {
        // Uncomment for getting netDumbster logs to the console
        // netDumbster.smtp.Logging.LogManager.GetLogger = type => new netDumbster.smtp.Logging.ConsoleLogger(type);
    }

    private void SendMail(EventHandler<MailSenderAfterSendEventArgs>? onAfterSend = null,
        EventHandler<MailSenderSmtpClientEventArgs>? onSmtpConnected = null,
        EventHandler<MailSenderSmtpClientEventArgs>? onSmtpDisconnected = null,
        EventHandler<MailSenderSendFailureEventArgs>? onSendFailure = null)
    {
        var data = new Dictionary<string, object>
        {
            {"MessageText", "This is just a sample plain text."},
            {"Date", DateTime.Now}
        };

        using var mmm = new MailMergeMessage("Mailsubject sent on {Date}", "{MessageText}")
            {Config = _settings.MessageConfig};
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "Test name", "test@example.com"));

        using var mms = new MailMergeSender {Config = _settings.SenderConfig};

        mms.OnAfterSend += onAfterSend;
        mms.OnSmtpConnected += onSmtpConnected;
        mms.OnSmtpDisconnected += onSmtpDisconnected;
        mms.OnSendFailure += onSendFailure;

        mms.Send(mmm, (object) data);
    }

    [Test]
    public void TryToSendWhenSenderIsBusy()
    {
        using var mmm = new MailMergeMessage("Subject", "plain text");
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "Test name", "test@example.com"));
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "Test name 2", "test2@example.com"));

        using var mms = new MailMergeSender
        {
            IsBusy = true
        };

        // single mail
        Assert.Throws<InvalidOperationException>(() => mms.Send(mmm, new object()));
        Assert.ThrowsAsync<InvalidOperationException>(async () => await mms.SendAsync(mmm, new object()));

        // several mails
        Assert.Throws<InvalidOperationException>(() => mms.Send(mmm, new Dictionary<string, string>()));
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await mms.SendAsync(mmm, new Dictionary<string, string>()));

        mms.IsBusy = false;
    }

    [Test]
    public void TryToSendWithNullMessage()
    {
        using var mms = new MailMergeSender();

        // single mail
        Assert.Throws<ArgumentNullException>(() => mms.Send(null!, new object()));
        Assert.ThrowsAsync<ArgumentNullException>(async () => await mms.SendAsync(null!, new object()));

        // several mails
        Assert.Throws<ArgumentNullException>(() => mms.Send(null!, new Dictionary<string, string>()));
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await mms.SendAsync(null!, new Dictionary<string, string>()));
    }

    [Test]
    public void TryToSendWithNullAsEnumerable()
    {
        using var mms = new MailMergeSender();
        using var mmm = new MailMergeMessage(); // no need to fully prepare for this test

        Assert.Throws<ArgumentNullException>(() => mms.Send(new MailMergeMessage(), (Dictionary<string, string>) null!));
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await mms.SendAsync(mmm, (Dictionary<string, string>) null!));
    }

    [Test]
    public void CancelSendOperationWithDelay()
    {
        using var mmm = new MailMergeMessage("Cancel with delay", "plain text");
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "Test name", "test@example.com"));
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "Test name 2", "test2@example.com"));

        using var mms = new MailMergeSender
        {
            Config = _settings.SenderConfig
        };
        mms.Config.MaxNumOfSmtpClients = 1;
        mms.Config.SmtpClientConfig[0].MessageOutput = MessageOutput.SmtpServer;
        mms.Config.SmtpClientConfig[0].DelayBetweenMessages = 2000;

        var anyData = new[] {new {mailNo = 1}, new {mailNo = 2}, new {mailNo = 3}};
            
        mms.SendCancel(500);
        Assert.ThrowsAsync<TaskCanceledException>(() => mms.SendAsync(mmm, anyData));
        Assert.AreEqual(0, _server?.ReceivedEmailCount);
    }

    [Test]
    public async Task CancelSendOperation()
    {
        using var mmm = new MailMergeMessage("Cancel immediately", "plain text");
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "Test name", "test@example.com"));
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "Test name 2", "test2@example.com"));

        using var mms = new MailMergeSender {Config = _settings.SenderConfig};
        mms.Config.MaxNumOfSmtpClients = 1;
        mms.Config.SmtpClientConfig[0].MessageOutput = MessageOutput.SmtpServer;
        mms.Config.SmtpClientConfig[0].DelayBetweenMessages = 2000;

        var anyData = new[] {new {mailNo = 1}, new {mailNo = 2}, new {mailNo = 3}};

        var tasks = new[]
        {
            await Task.Factory.StartNew(async () => await mms.SendAsync(mmm, anyData)),
            Task.Factory.StartNew(() => mms.SendCancel()),
            Task.Factory.StartNew(() => mms.SendCancel()) // a second cancel operation will just return
        };

        Assert.Throws<AggregateException>(() => { Task.WaitAll(tasks); });

        Assert.AreEqual(0, _server?.ReceivedEmailCount);
    }

    [Test]
    public void SendMailWithStandardConfig()
    {
        var connectedCounter = 0;
        var disconnectedCounter = 0;
        SmtpClientConfig? usedClientConfig = null;

        void OnAfterSend(object? sender, MailSenderAfterSendEventArgs args)
        {
            lock (_locker)
            {
                usedClientConfig = args.SmtpClientConfig;
            }
        }

        void OnSmtpConnected(object? sender, MailSenderSmtpClientEventArgs args)
        {
            lock (_locker)
            {
                connectedCounter++;
            }
        }

        void OnSmtpDisconnected(object? sender, MailSenderSmtpClientEventArgs args)
        {
            lock (_locker)
            {
                disconnectedCounter++;
            }
        }

        SendMail(OnAfterSend, OnSmtpConnected, OnSmtpDisconnected);

        Assert.AreEqual(1, connectedCounter);
        Assert.AreEqual(1, disconnectedCounter);
        Assert.AreEqual(1, _server?.ReceivedEmailCount);
        Assert.AreEqual(_settings.SenderConfig.SmtpClientConfig[0].Name, usedClientConfig?.Name);

        Console.WriteLine($"Sending mail with smtp config name '{usedClientConfig?.Name}' passed.\n\n");
        Console.WriteLine(_server?.ReceivedEmail[0].Data);
    }

    [Test]
    public void SendMailWithBackupConfig()
    {
        SmtpClientConfig? usedClientConfig = null;

        void OnAfterSend(object? sender, MailSenderAfterSendEventArgs args)
        {
            usedClientConfig = args.SmtpClientConfig;
        }

        _settings.SenderConfig.SmtpClientConfig[0]
            .SmtpPort++; // set wrong server port, so that backup config should be taken
        SendMail(OnAfterSend);
        Assert.AreEqual(1, _server?.ReceivedEmailCount);
        Assert.AreEqual(_settings.SenderConfig.SmtpClientConfig[1].Name, usedClientConfig?.Name);

        Console.WriteLine($"Sending mail with smtp config name '{usedClientConfig?.Name}' passed.\n\n");
        Console.WriteLine(_server?.ReceivedEmail[0].Data);
    }

    [Test]
    public void SendMailWithSendFailure()
    {
        SmtpClientConfig? usedClientConfig = null;
        Exception? sendFailure = null;

        void OnSendFailure(object? sender, MailSenderSendFailureEventArgs args)
        {
            lock (_locker)
            {
                sendFailure = args.Error;
                usedClientConfig = args.SmtpClientConfig;
            }
        }

        _settings.SenderConfig.SmtpClientConfig[0]
            .SmtpPort++; // set wrong server port, so that backup config should be taken
        _settings.SenderConfig.SmtpClientConfig[1].SmtpPort++; // set wrong server port, so that send will fail
        Assert.Catch(() => SendMail(onSendFailure: OnSendFailure));
        Assert.AreEqual(_settings.SenderConfig.SmtpClientConfig[1].Name, usedClientConfig?.Name);
        Assert.AreEqual(0, _server?.ReceivedEmailCount);
    }

    private class Recipient
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

    }

    [TestCase("", false)]
    [TestCase("{CauseParseFailure", true)]
    [TestCase("{CauseMissingVariableFailure}", true)]
    public async Task AllSenderEventsSingleMail(string somePlaceholder, bool withParseFailure)
    {
        #region * Sync and Async preparation *

        var actualEvents = new ConcurrentStack<string>();
        var expectedEvents = new ConcurrentStack<string>();

        using var mms = new MailMergeSender {Config = _settings.SenderConfig};
        mms.Config.MaxNumOfSmtpClients = 1;

        // Event raising when getting the merged MimeMessage of the MailMergeMessage has failed.
        mms.OnMessageFailure += (mailMergeSender, messageFailureArgs) =>
        {
            actualEvents.Push(nameof(mms.OnMessageFailure));
        };

        // Event raising before sending a single mail message starts
        mms.OnBeforeSend += (smtpClient, beforeSendArgs) => { actualEvents.Push(nameof(mms.OnBeforeSend)); };

        // Event raising right after the SmtpClient's connection to the server is up (but not yet authenticated).
        mms.OnSmtpConnected += (smtpClient, smtpClientArgs) => { actualEvents.Push(nameof(mms.OnSmtpConnected)); };
        // Event raising after the SmtpClient has authenticated on the server.
        mms.OnSmtpAuthenticated += (smtpClient, smtpClientArgs) =>
        {
            actualEvents.Push(nameof(mms.OnSmtpAuthenticated));
        };
        // Event raising after the SmtpClient has disconnected from the SMTP mail server.
        mms.OnSmtpDisconnected += (smtpClient, smtpClientArgs) =>
        {
            actualEvents.Push(nameof(mms.OnSmtpDisconnected));
        };

        // Event raising if sending a single mail message fails
        mms.OnSendFailure += (smtpClient, sendFailureArgs) => { actualEvents.Push(nameof(mms.OnSendFailure)); };
        // Event raising before sending a single mail message is finished
        mms.OnAfterSend += (smtpClient, afterSendArgs) => { actualEvents.Push(nameof(mms.OnAfterSend)); };

        var recipient = new Recipient {Email = $"recipient@example.com", Name = $"Name of recipient"};

        using var mmm = new MailMergeMessage("Event tests" + somePlaceholder,
            "This is the plain text part for {Name} ({Email})") {Config = _settings.MessageConfig};

        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "{Name}", "{Email}"));

        var sequenceOfExpectedEvents = new List<string>();
        if (withParseFailure)
        {
            sequenceOfExpectedEvents.Clear();
            sequenceOfExpectedEvents.AddRange(new[]
            {
                nameof(mms.OnMessageFailure) /*,
                    nameof(mms.OnSmtpDisconnected)*/
            });
        }
        else
        {
            sequenceOfExpectedEvents.Clear();
            sequenceOfExpectedEvents.AddRange(new[]
            {
                nameof(mms.OnBeforeSend), nameof(mms.OnSmtpConnected), nameof(mms.OnSmtpAuthenticated),
                nameof(mms.OnAfterSend), nameof(mms.OnSmtpDisconnected)
            });
        }

        #endregion

        #region * Synchronous send method *

        try
        {
            mms.Send(mmm, recipient);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        expectedEvents.Clear();
        expectedEvents.PushRange(sequenceOfExpectedEvents.ToArray());

        Assert.AreEqual(expectedEvents.Count, actualEvents.Count);
        // sequence of sync sending is predefined
        while (actualEvents.Count > 0)
        {
            expectedEvents.TryPop(out var expected);
            actualEvents.TryPop(out var actual);
            Assert.AreEqual(expected, actual);
        }

        #endregion

        #region * Async send method *

        actualEvents.Clear();
        expectedEvents.Clear();
        expectedEvents.PushRange(sequenceOfExpectedEvents.ToArray());

        try
        {
            await mms.SendAsync(mmm, recipient);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        Assert.AreEqual(expectedEvents.Count, actualEvents.Count);

        // sequence of async sending may be different from sync, but all events must exists
        var sortedActual = actualEvents.OrderBy(e => e).ToArray();
        var sortedExpected = expectedEvents.OrderBy(e => e).ToArray();

        for (var i = 0; i < sortedActual.Length; i++)
        {
            Assert.AreEqual(sortedExpected[i], sortedActual[i]);
        }

        #endregion
    }

    [TestCase("", false)]
    [TestCase("{CauseParseFailure", true)]
    [TestCase("{CauseMissingVariableFailure}", true)]
    public async Task AllSenderEventsMailMerge(string somePlaceholder, bool withParseFailure)
    {
        #region * Sync and Async preparation *

        var actualEvents = new ConcurrentStack<string>();
        var expectedEvents = new ConcurrentStack<string>();

        using var mms = new MailMergeSender {Config = _settings.SenderConfig};
        mms.Config.MaxNumOfSmtpClients = 1;

        // Event raising before merging starts
        mms.OnMergeBegin += (mailMergeSender, mergeBeginArgs) => { actualEvents.Push(nameof(mms.OnMergeBegin)); };
        // Event raising when getting the merged MimeMessage of the MailMergeMessage has failed.
        mms.OnMessageFailure += (mailMergeSender, messageFailureArgs) =>
        {
            actualEvents.Push(nameof(mms.OnMessageFailure));
        };

        // Event raising before sending a single mail message starts
        mms.OnBeforeSend += (smtpClient, beforeSendArgs) => { actualEvents.Push(nameof(mms.OnBeforeSend)); };

        // Event raising right after the SmtpClient's connection to the server is up (but not yet authenticated).
        mms.OnSmtpConnected += (smtpClient, smtpClientArgs) => { actualEvents.Push(nameof(mms.OnSmtpConnected)); };
        // Event raising after the SmtpClient has authenticated on the server.
        mms.OnSmtpAuthenticated += (smtpClient, smtpClientArgs) =>
        {
            actualEvents.Push(nameof(mms.OnSmtpAuthenticated));
        };
        // Event raising after the SmtpClient has disconnected from the SMTP mail server.
        mms.OnSmtpDisconnected += (smtpClient, smtpClientArgs) =>
        {
            actualEvents.Push(nameof(mms.OnSmtpDisconnected));
        };

        // Event raising if sending a single mail message fails
        mms.OnSendFailure += (smtpClient, sendFailureArgs) => { actualEvents.Push(nameof(mms.OnSendFailure)); };
        // Event raising before sending a single mail message is finished
        mms.OnAfterSend += (smtpClient, afterSendArgs) => { actualEvents.Push(nameof(mms.OnAfterSend)); };

        // Event raising each time before and after a single message was sent
        mms.OnMergeProgress += (mailMergeSender, progressArgs) =>
        {
            actualEvents.Push(nameof(mms.OnMergeProgress));
        };
        // Event raising after merging is completed
        mms.OnMergeComplete += (mailMergeSender, completedArgs) =>
        {
            actualEvents.Push(nameof(mms.OnMergeComplete));
        };

        var recipients = new List<Recipient>();
        for (var i = 0; i < 1; i++)
        {
            recipients.Add(new Recipient {Email = $"recipient-{i}@example.com", Name = $"Name of {i}"});
        }

        using var mmm = new MailMergeMessage("Event tests" + somePlaceholder,
            "This is the plain text part for {Name} ({Email})") {Config = _settings.MessageConfig};

        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "{Name}", "{Email}"));

        var sequenceOfExpectedEvents = new List<string>();
        if (withParseFailure)
        {
            sequenceOfExpectedEvents.Clear();
            sequenceOfExpectedEvents.AddRange(new[]
            {
                nameof(mms.OnMergeBegin), nameof(mms.OnMergeProgress), nameof(mms.OnMessageFailure),
                nameof(mms.OnMergeProgress), nameof(mms.OnMergeComplete)
            });
        }
        else
        {
            sequenceOfExpectedEvents.Clear();
            sequenceOfExpectedEvents.AddRange(new[]
            {
                nameof(mms.OnMergeBegin), nameof(mms.OnMergeProgress), nameof(mms.OnBeforeSend),
                nameof(mms.OnSmtpConnected), nameof(mms.OnSmtpAuthenticated),
                nameof(mms.OnAfterSend), nameof(mms.OnMergeProgress), nameof(mms.OnSmtpDisconnected),
                nameof(mms.OnMergeComplete)
            });
        }

        #endregion

        #region * Synchronous send method *

        try
        {
            mms.Send(mmm, recipients);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        expectedEvents.Clear();
        expectedEvents.PushRange(sequenceOfExpectedEvents.ToArray());

        Assert.AreEqual(expectedEvents.Count, actualEvents.Count);
        // sequence of sync sending is predefined
        while (actualEvents.Count > 0)
        {
            expectedEvents.TryPop(out var expected);
            actualEvents.TryPop(out var actual);
            Assert.AreEqual(expected, actual);
        }

        #endregion

        #region * Async send method *

        actualEvents.Clear();
        expectedEvents.Clear();
        expectedEvents.PushRange(sequenceOfExpectedEvents.ToArray());

        try
        {
            await mms.SendAsync(mmm, recipients);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        Assert.AreEqual(expectedEvents.Count, actualEvents.Count);

        // sequence of async sending may be different from sync, but all events must exists
        var sortedActual = actualEvents.OrderBy(e => e).ToArray();
        var sortedExpected = expectedEvents.OrderBy(e => e).ToArray();

        for (var i = 0; i < sortedActual.Length; i++)
        {
            Assert.AreEqual(sortedExpected[i], sortedActual[i]);
        }

        #endregion

    }

    [TestCase(true, true)] // setting for setMimeMessageToNull is irrelevant
    [TestCase(false, false)] // no exception is fine ONLY if MimeMessage is not null
    [TestCase(false, true)] // no exception with null for MimeMessage must throw exception
    public void Send_With_And_Without_MailMergeMessageException(bool throwException, bool setMimeMessageToNull)
    {
        #region * Sync and Async preparation *

        const string theFormatError = "{causeFormatError}";
        const string plainText = theFormatError + "This is the plain text part for {Name} ({Email})";

        using var mms = new MailMergeSender {Config = _settings.SenderConfig};
        mms.Config.MaxNumOfSmtpClients = 1;

        // Event raising when getting the merged MimeMessage of the MailMergeMessage has failed.
        mms.OnMessageFailure += (mailMergeSender, messageFailureArgs) =>
        {
            lock (_locker)
            {
                if (throwException)
                {
                    return;
                }

                // Remove the cause of the exception and return corrected values
                // Note: changes of MailMergeMessage will affect als messages to be sent
                messageFailureArgs.MailMergeMessage.PlainText = plainText.Replace(theFormatError, string.Empty);
                // in production a try...catch... must be implemented
                if (setMimeMessageToNull)
                {
                    messageFailureArgs.MimeMessage = null;
                }
                else
                {
                    messageFailureArgs.MimeMessage =
                        messageFailureArgs.MailMergeMessage.GetMimeMessage(messageFailureArgs.DataSource);
                }

                messageFailureArgs.ThrowException = throwException;
            }
        };

        var recipients = new List<Recipient>();
        for (var i = 0; i < 2; i++)
        {
            recipients.Add(new Recipient {Email = $"recipient-{i}@example.com", Name = $"Name of {i}"});
        }

        using var mmm = new MailMergeMessage("Message failure", plainText) {Config = _settings.MessageConfig};
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "{Name}", "{Email}"));

        #endregion

        #region * Synchronous send methods *

        // send enumerable data
        mmm.PlainText = plainText; // set text from constant
        try
        {
            if (throwException)
            {
                Assert.Throws<MailMergeMessage.MailMergeMessageException>(() => mms.Send(mmm, recipients));
            }
            else
            {
                if (setMimeMessageToNull)
                {
                    Assert.Throws<MailMergeMessage.MailMergeMessageException>(() => mms.Send(mmm, recipients));
                }
                else
                {
                    Assert.DoesNotThrow(() => mms.Send(mmm, recipients));
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        if (throwException)
        {
            Assert.AreEqual(0, _server?.ReceivedEmailCount);
        }
        else
        {
            if (setMimeMessageToNull)
            {
                Assert.AreEqual(0, _server?.ReceivedEmailCount);
            }
            else
            {
                Assert.AreEqual(recipients.Count, _server?.ReceivedEmailCount);
            }
        }

        _server?.ClearReceivedEmail();

        // send single data item
        mmm.PlainText = plainText; // set text from constant
        try
        {
            if (throwException)
            {
                Assert.Throws<MailMergeMessage.MailMergeMessageException>(() => mms.Send(mmm, recipients[0]));
            }
            else
            {
                if (setMimeMessageToNull)
                {
                    Assert.Throws<MailMergeMessage.MailMergeMessageException>(() => mms.Send(mmm, recipients[0]));
                }
                else
                {
                    Assert.DoesNotThrow(() => mms.Send(mmm, recipients[0]));
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        if (throwException)
        {
            Assert.AreEqual(0, _server?.ReceivedEmailCount);
        }
        else
        {
            if (setMimeMessageToNull)
            {
                Assert.AreEqual(0, _server?.ReceivedEmailCount);
            }
            else
            {
                Assert.AreEqual(1, _server?.ReceivedEmailCount);
            }
        }

        _server?.ClearReceivedEmail();

        #endregion

        #region * Async send methods *

        // send enumerable data
        mmm.PlainText = plainText; // set text from constant
        try
        {
            if (throwException)
            {
                Assert.ThrowsAsync<MailMergeMessage.MailMergeMessageException>(async () =>
                    await mms.SendAsync(mmm, recipients));
            }
            else
            {
                if (setMimeMessageToNull)
                {
                    Assert.ThrowsAsync<MailMergeMessage.MailMergeMessageException>(async () =>
                        await mms.SendAsync(mmm, recipients));
                }
                else
                {
                    Assert.DoesNotThrow(() => mms.Send(mmm, recipients));
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        if (throwException)
        {
            Assert.AreEqual(0, _server?.ReceivedEmailCount);
        }
        else
        {
            if (setMimeMessageToNull)
            {
                Assert.AreEqual(0, _server?.ReceivedEmailCount);
            }
            else
            {
                Assert.AreEqual(recipients.Count, _server?.ReceivedEmailCount);
            }
        }

        _server?.ClearReceivedEmail();

        // send single data item
        mmm.PlainText = plainText; // set text from constant
        try
        {
            if (throwException)
            {
                Assert.ThrowsAsync<MailMergeMessage.MailMergeMessageException>(async () =>
                    await mms.SendAsync(mmm, recipients[0]));
            }
            else
            {
                if (setMimeMessageToNull)
                {
                    Assert.ThrowsAsync<MailMergeMessage.MailMergeMessageException>(async () =>
                        await mms.SendAsync(mmm, recipients[0]));
                }
                else
                {
                    Assert.DoesNotThrow(() => mms.Send(mmm, recipients[0]));
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        if (throwException)
        {
            Assert.AreEqual(0, _server?.ReceivedEmailCount);
        }
        else
        {
            if (setMimeMessageToNull)
            {
                Assert.AreEqual(0, _server?.ReceivedEmailCount);
            }
            else
            {
                Assert.AreEqual(1, _server?.ReceivedEmailCount);
            }
        }

        #endregion
    }

    [TestCase(10)]
    //[TestCase(1000, Ignore = "Only for performance tests")]
    public async Task SendSyncAndAsyncPerformance(int numOfRecipients)
    {
        // In this sample:
        // With 100,000 messages and 10 MaxNumOfSmtpClients async is about twice as fast as sync.

        var recipients = new List<Recipient>();
        for (var i = 0; i < numOfRecipients; i++)
        {
            recipients.Add(new Recipient {Email = $"recipient-{i}@example.com", Name = $"Name of {i}"});
        }

        using var mmm = new MailMergeMessage("Async/Sync email test", "This is the plain text part for {Name} ({Email})")
            {Config = _settings.MessageConfig};
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "{Name}", "{Email}"));
        using var mms = new MailMergeSender {Config = _settings.SenderConfig};

        mms.Config.MaxNumOfSmtpClients = 10;
        var sw = new Stopwatch();

        sw.Start();
        await mms.SendAsync(mmm, recipients);
        sw.Stop();
        Console.WriteLine($"Time to send {recipients.Count} messages sync: {sw.ElapsedMilliseconds} milliseconds.");
        Console.WriteLine();
        Assert.AreEqual(recipients.Count, _server?.ReceivedEmail.Length);
        Assert.IsFalse(mms.IsBusy);

        sw.Reset();
        _server?.ClearReceivedEmail();

        sw.Start();

        var numOfSmtpClientsUsed = 0;
        mms.OnMergeComplete += (s, args) => { numOfSmtpClientsUsed = args.NumOfSmtpClientsUsed; };

        await mms.SendAsync(mmm, recipients);
        sw.Stop();
        Console.WriteLine(
            $"Time to send {recipients.Count} messages async: {sw.ElapsedMilliseconds} milliseconds.");

        // Note: With too many SmtpClients and small emails some of the clients will never de-queue from the ConcurrentQueue of MailMergeSender
        Console.WriteLine(
            $"{numOfSmtpClientsUsed} tasks (and SmtpClients) used for sending async\n(max {mms.Config.MaxNumOfSmtpClients} were configured).");

        Assert.AreEqual(recipients.Count, _server?.ReceivedEmail.Length);
        Assert.IsFalse(mms.IsBusy);
    }

    #region *** Test setup ***

    [OneTimeSetUp]
    public void FixtureSetUp()
    {
    }

    [OneTimeTearDown]
    public void FixtureTearDown()
    {
    }

    [SetUp]
    public void SetUp()
    {
        _server?.ClearReceivedEmail();
        _server?.Stop();

        // Server frequently hangs, if not initialized before each test
        _server = SimpleSmtpServer.Start(Helper.GetFreeTcpPort());
        _settings = GetSettings();
    }

    private static Settings GetSettings()
    {
        return new Settings
        {
            MessageConfig =
            {
                CharacterEncoding = Encoding.UTF8,
                StandardFromAddress = new MailboxAddress("sender name", "sender@example.com"),
                CultureInfo = CultureInfo.CurrentCulture,
                IgnoreIllegalRecipientAddresses = true,
                SmartFormatterConfig =
                {
                    FormatErrorAction = ErrorAction.Ignore,
                    ParseErrorAction = ErrorAction.ThrowError
                },
                Xmailer = "MailMergeLib 5"
            },
            SenderConfig =
            {
                MaxNumOfSmtpClients = 5,

                SmtpClientConfig = new[]
                {
                    new SmtpClientConfig
                    {
                        MessageOutput = MessageOutput.SmtpServer,
                        SmtpHost = "localhost",
                        SmtpPort = (int)_server?.Configuration.Port!,
                        NetworkCredential = new Credential("user", "pwd"), // not used for netDumbster
                        SecureSocketOptions = SecureSocketOptions.Auto,
                        Name = "Standard",
                        MaxFailures = 3,
                        DelayBetweenMessages = 0,
                        ClientDomain = "mail.mailmergelib.net"
                    },
                    new SmtpClientConfig
                    {
                        MessageOutput = MessageOutput.SmtpServer,
                        SmtpHost = "localhost",
                        SmtpPort = _server.Configuration.Port,
                        NetworkCredential = new Credential("user", "pwd"), // not used for netDumbster
                        SecureSocketOptions = SecureSocketOptions.None,
                        Name = "Backup",
                        MaxFailures = 3,
                        DelayBetweenMessages = 0,
                        ClientDomain = "mail.mailmergelib.net"
                    }
                }
            }
        };
    }

    #endregion
}