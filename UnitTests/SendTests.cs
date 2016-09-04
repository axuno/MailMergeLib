using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MailKit.Security;
using MailMergeLib;
using MailMergeLib.SmartFormatMail.Core.Settings;
using MimeKit;
using netDumbster.smtp;
using netDumbster.smtp.Logging;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class SendTests
    {
        private static SimpleSmtpServer _server;
        private Random _rnd = new Random();
	    private Settings _settings = new Settings();
	    
        public SendTests()
        {
	        // LogManager.GetLogger = type => new ConsoleLogger(type);
        }

        private void SendMail(EventHandler<MailSenderAfterSendEventArgs> onAfterSend = null)
        {
	        var data = new Dictionary<string, object>();
			data.Add("MessageText", "This is just a sample plain text.");
			data.Add("Date", DateTime.Now);

	        var mmm = new MailMergeMessage("Mailsubject sent on {Date}", "{MessageText}") {Config = _settings.MessageConfig};
			mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "Test name", "test@example.com"));

			var mms = new MailMergeSender() {Config = _settings.SenderConfig};

			if (onAfterSend != null)
				mms.OnAfterSend += onAfterSend;

			mms.Send(mmm, (object) data);
        }

		[Test]
		public void SendMailWithStandardConfig()
		{
			SmtpClientConfig usedClientConfig = null;
			EventHandler<MailSenderAfterSendEventArgs> onAfterSend = (sender, args) => { usedClientConfig = args.SmtpClientConfig; };

			SendMail(onAfterSend);

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

		#region *** Test setup ***

		[TestFixtureSetUp]
        public void FixtureSetUp()
        {
			_server = SimpleSmtpServer.Start(_rnd.Next(50000, 60000));
        }

        [TestFixtureTearDown]
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
							DelayBetweenMessages = 1000,
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
							DelayBetweenMessages = 1000,
							ClientDomain = "mail.mailmergelib.net"
						}
					}
				}
			};
		}
		#endregion
	}
}
