using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MailKit.Security;
using MailMergeLib;
using MailMergeLib.SmartFormatMail.Core.Settings;
using MimeKit;
using netDumbster.smtp;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class Sender_EventsAndSend
    {
        private static SimpleSmtpServer _server;
        private Random _rnd = new Random();
        private Settings _settings = new Settings();
        
        public Sender_EventsAndSend()
        {
            // Uncomment for getting netDumbster logs to the console
            // netDumbster.smtp.Logging.LogManager.GetLogger = type => new netDumbster.smtp.Logging.ConsoleLogger(type);
        }

        private void SendMail(EventHandler<MailSenderAfterSendEventArgs> onAfterSend = null, EventHandler<MailSenderSmtpClientEventArgs> onSmtpConnected = null, EventHandler<MailSenderSmtpClientEventArgs> onSmtpDisconnected = null)
        {
            var data = new Dictionary<string, object>
            {
                {"MessageText", "This is just a sample plain text."},
                {"Date", DateTime.Now}
            };

            var mmm = new MailMergeMessage("Mailsubject sent on {Date}", "{MessageText}") {Config = _settings.MessageConfig};
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "Test name", "test@example.com"));

            var mms = new MailMergeSender() {Config = _settings.SenderConfig};

            mms.OnAfterSend += onAfterSend;
            mms.OnSmtpConnected += onSmtpConnected;
            mms.OnSmtpDisconnected += onSmtpDisconnected;
           
            mms.Send(mmm, (object) data);
        }

        [Test]
        public void SendMailWithStandardConfig()
        {
            var connCounter = 0;
            var disconnCounter = 0;
            SmtpClientConfig usedClientConfig = null;
            EventHandler<MailSenderAfterSendEventArgs> onAfterSend = (sender, args) => { usedClientConfig = args.SmtpClientConfig; };
            EventHandler<MailSenderSmtpClientEventArgs> onSmtpConnected = (sender, args) => { connCounter++;};
            EventHandler<MailSenderSmtpClientEventArgs> onSmtpDisconnected = (sender, args) => { disconnCounter++; };

            SendMail(onAfterSend, onSmtpConnected, onSmtpDisconnected);

            Assert.AreEqual(connCounter, 1);
            Assert.AreEqual(disconnCounter, 1);
            Assert.AreEqual(1, _server.ReceivedEmailCount);
            Assert.AreEqual(_settings.SenderConfig.SmtpClientConfig[0].Name, usedClientConfig.Name);

            Console.WriteLine($"Sending mail with smtp config name '{usedClientConfig.Name}' passed.\n\n");
            Console.WriteLine(_server.ReceivedEmail[0].Data);
        }

        [Test]
        public void SendMailWithBackupConfig()
        {
            SmtpClientConfig usedClientConfig = null;
            EventHandler<MailSenderAfterSendEventArgs> onAfterSend = (sender, args) => { usedClientConfig = args.SmtpClientConfig; };

            _settings.SenderConfig.SmtpClientConfig[0].SmtpPort++; // set wrong server port, so that backup config should be taken
            SendMail(onAfterSend);
            Assert.AreEqual(1, _server.ReceivedEmailCount);
            Assert.AreEqual(_settings.SenderConfig.SmtpClientConfig[1].Name, usedClientConfig.Name);

            Console.WriteLine($"Sending mail with smtp config name '{usedClientConfig.Name}' passed.\n\n");
            Console.WriteLine(_server.ReceivedEmail[0].Data);
        }

        private class Recipient
        {
            public string Name { get; set; }
            public string Email { get; set; }

        }

        [Test]
        public async Task AllSenderEvents()
        {
            var actualEvents = new ConcurrentStack<string>();
            var expectedEvents = new ConcurrentStack<string>();

            var mms = new MailMergeSender { Config = _settings.SenderConfig };
            mms.Config.MaxNumOfSmtpClients = 1;

            // Event raising before merging starts
            mms.OnMergeBegin += (mailMergeSender, mergeBeginArgs) => { actualEvents.Push(nameof(mms.OnMergeBegin)); };
            // Event raising when getting the merged MimeMessage of the MailMergeMessage has failed.
            mms.OnMessageFailure += (mailMergeSender, messageFailureArgs) => { actualEvents.Push(nameof(mms.OnMessageFailure)); };

            // Event raising before sending a single mail message starts
            mms.OnBeforeSend += (smtpClient, beforeSendArgs) => { };

            // Event raising right after the SmtpClient's connection to the server is up (but not yet authenticated).
            mms.OnSmtpConnected += (smtpClient, smtpClientArgs) => { actualEvents.Push(nameof(mms.OnSmtpConnected)); };
            // Event raising after the SmtpClient has authenticated on the server.
            mms.OnSmtpAuthenticated += (smtpClient, smtpClientArgs) => { actualEvents.Push(nameof(mms.OnSmtpAuthenticated)); }; 
            // Event raising after the SmtpClient has disconnected from the SMTP mail server.
            mms.OnSmtpDisconnected += (smtpClient, smtpClientArgs) => { actualEvents.Push(nameof(mms.OnSmtpDisconnected)); };

            // Event raising if sending a single mail message fails
            mms.OnSendFailure += (smtpClient, sendFailureArgs) => { actualEvents.Push(nameof(mms.OnSendFailure)); };
            // Event raising before sending a single mail message is finished
            mms.OnAfterSend += (smtpClient, afterSendArgs) => { actualEvents.Push(nameof(mms.OnAfterSend)); };

            // Event raising each time before and after a single message was sent
            mms.OnMergeProgress += (mailMergeSender, progressArgs) => { actualEvents.Push(nameof(mms.OnMergeProgress)); };
            // Event raising after merging is completed
            mms.OnMergeComplete += (mailMergeSender, completedArgs) => { actualEvents.Push(nameof(mms.OnMergeComplete)); };

            var recipients = new List<Recipient>();
            for (var i = 0; i < 1; i++)
            {
                recipients.Add(new Recipient() { Email = $"recipient-{i}@example.com", Name = $"Name of {i}" });
            }

            var mmm = new MailMergeMessage("Event tests", "This is the plain text part for {Name} ({Email})") { Config = _settings.MessageConfig };
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "{Name}", "{Email}"));

            var sequenceOfExpectedEvents = new[] { nameof(mms.OnMergeBegin), nameof(mms.OnMergeProgress), nameof(mms.OnSmtpConnected), nameof(mms.OnAfterSend), nameof(mms.OnMergeProgress), nameof(mms.OnMergeComplete), nameof(mms.OnSmtpDisconnected) };

            #region * Synchronous send method *

            mms.Send(mmm, recipients);

            expectedEvents.Clear();
            expectedEvents.PushRange(sequenceOfExpectedEvents);

            Assert.AreEqual(expectedEvents.Count, actualEvents.Count);
            // sequence of sync sending is predefined
            for (var i = 0; i < actualEvents.Count; i++)
            {
                expectedEvents.TryPop(out string expected);
                actualEvents.TryPop(out string actual);
                Assert.AreEqual(expected, actual);
            }

            #endregion

            #region * Async send method *

            actualEvents.Clear();
            expectedEvents.Clear();
            expectedEvents.PushRange(sequenceOfExpectedEvents);

            await mms.SendAsync(mmm, recipients);
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


        [Test]
        public async Task SendSyncAndAsyncPerformance()
        {
            // In this sample:
            // With 100,000 messages and 10 MaxNumOfSmtpClients async is about twice as fast as sync.

            var recipients = new List<Recipient>();
            for (var i = 0; i < 1000; i++)
            {
                recipients.Add(new Recipient() {Email = $"recipient-{i}@example.com", Name = $"Name of {i}"});
            }
            
            var mmm = new MailMergeMessage("Async/Sync email test", "This is the plain text part for {Name} ({Email})") { Config = _settings.MessageConfig };
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "{Name}", "{Email}"));
            var mms = new MailMergeSender { Config = _settings.SenderConfig };

            mms.Config.MaxNumOfSmtpClients = 10;
            var sw = new Stopwatch();

            sw.Start();
            mms.Send(mmm, recipients);
            sw.Stop();
            Console.WriteLine($"Time to send {recipients.Count} messages sync: {sw.ElapsedMilliseconds} milliseconds.");
            Console.WriteLine();
            Assert.AreEqual(recipients.Count, _server.ReceivedEmail.Length);
            Assert.IsFalse(mms.IsBusy);

            sw.Reset();
            _server.ClearReceivedEmail();

            sw.Start();

            int numOfSmtpClientsUsed = 0;
            mms.OnMergeComplete += (s, args) => { numOfSmtpClientsUsed = args.NumOfSmtpClientsUsed; };

            await mms.SendAsync(mmm, recipients);
            sw.Stop();
            Console.WriteLine($"Time to send {recipients.Count} messages async: {sw.ElapsedMilliseconds} milliseconds.");
            
            // Note: With too many SmtpClients and small emails some of the clients will never de-queue from the ConcurrentQueue of MailMergeSender
            Console.WriteLine($"{numOfSmtpClientsUsed} tasks (and SmtpClients) used for sending async\n(max {mms.Config.MaxNumOfSmtpClients} were configured).");
            
            Assert.AreEqual(recipients.Count, _server.ReceivedEmail.Length);
            Assert.IsFalse(mms.IsBusy);
        }

        #region *** Test setup ***

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            _server = SimpleSmtpServer.Start(_rnd.Next(50000, 60000));
        }

        [OneTimeTearDown]
        public void FixtureTearDown()
        {
            _server.Stop();
        }

        [SetUp]
        public void SetUp()
        {
            _settings = GetSettings();
            _server.ClearReceivedEmail();
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
                        new SmtpClientConfig()
                        {
                            MessageOutput = MessageOutput.SmtpServer,
                            SmtpHost = "localhost",
                            SmtpPort = _server.Port,
                            NetworkCredential = new Credential("user", "pwd"), // not used for netDumbster
                            SecureSocketOptions = SecureSocketOptions.None,
                            Name = "Standard",
                            MaxFailures = 3,
                            DelayBetweenMessages = 0,
                            ClientDomain = "mail.mailmergelib.net"
                        },
                        new SmtpClientConfig()
                        {
                            MessageOutput = MessageOutput.SmtpServer,
                            SmtpHost = "localhost",
                            SmtpPort = _server.Port,
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
}
