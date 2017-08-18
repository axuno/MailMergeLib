using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Text;
using MailKit.Security;
using MailMergeLib;
using MailMergeLib.SmartFormatMail.Core.Settings;
using MimeKit;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class Settings_Serialization
    {
        private Settings _outSettings;

        [OneTimeSetUp]
        public void Setup()
        {
            // initialize settings with non-default values

            Settings.CryptoKey = "SomeSecretCryptoKey";
            _outSettings = new Settings
            {
                MessageConfig =
                {
                    CharacterEncoding = Encoding.UTF32,
                    StandardFromAddress = new MailboxAddress("sender name", "sender@example.com"),
                    CultureInfo = new CultureInfo("de-DE"),
                    IgnoreIllegalRecipientAddresses = false,
                    IgnoreMissingInlineAttachments = true,
                    IgnoreMissingFileAttachments = true,
                    Organization = "axuno gGmbH",
                    Priority = MessagePriority.Urgent,
                    SmartFormatterConfig =
                    {
                        FormatErrorAction = ErrorAction.OutputErrorInResult,
                        ParseErrorAction = ErrorAction.MaintainTokens,
                        CaseSensitivity = CaseSensitivityType.CaseInsensitive,
                        ConvertCharacterStringLiterals = true
                    },
                    Xmailer = "MailMergeLib 5",
                    FileBaseDirectory = "Path-to-Base-Dir",
                    TextTransferEncoding = ContentEncoding.QuotedPrintable,
                    BinaryTransferEncoding = ContentEncoding.UUEncode
                },
                SenderConfig =
                {
                    SmtpClientConfig = new[]
                    {
                        new SmtpClientConfig()
                        {
                            MessageOutput = MessageOutput.SmtpServer,
                            SmtpHost = "some.host.com",
                            SmtpPort = 123,
                            NetworkCredential = new Credential("user", "password"),
                            Name = "Best",
                            MaxFailures = 12,
                            RetryDelayTime = 1234,
                            DelayBetweenMessages = 543,
                            SecureSocketOptions = SecureSocketOptions.StartTlsWhenAvailable,
                            LocalEndPoint = new IPEndPoint(12345, 123),
                            SslProtocols = SslProtocols.Ssl3,
                            ClientDomain = "TestDomain",
                            MailOutputDirectory = "Path-to-folder",
                            Timeout = 4321
                        },
                        new SmtpClientConfig()
                        {
                            MessageOutput = MessageOutput.SmtpServer,
                            SmtpHost = "some.otherhost.com",
                            SmtpPort = 25,
                            NetworkCredential = new Credential("user2", "password2"),
                            Name = "Next best",
                            DelayBetweenMessages = 2000
                        }
                    },
                    MaxNumOfSmtpClients = 5
                }
            };
        }

        [Test]
        public void Settings_Save_and_Restore()
        {
            var outMs = new MemoryStream();

            _outSettings.Serialize(outMs, Encoding.UTF8);
            var inSettings = Settings.Deserialize(outMs, Encoding.UTF8);

            Assert.IsTrue(inSettings.SenderConfig.Equals(_outSettings.SenderConfig));
            outMs.Dispose();
        }
    }
}
