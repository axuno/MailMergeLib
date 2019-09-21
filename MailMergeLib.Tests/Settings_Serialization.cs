using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Text;
using MailKit.Security;
using MimeKit;
using NUnit.Framework;
using SmartFormat.Core.Settings;

namespace MailMergeLib.Tests
{
    [TestFixture]
    public class Settings_Serialization
    {
        private const string _settingsFilename = "TestSettings.xml";
        private Settings _outSettings;

        [OneTimeSetUp]
        public void Setup()
        {
            // initialize settings with non-default values

            Settings.CryptoKey = "SomeSecretCryptoKey";
            Settings.CryptoEnabled = false;

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
                    FileBaseDirectory = "C:\\Path-to-Base-Dir", // must be a full path, not necessarily existing
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
            _outSettings.Serialize(Path.Combine(TestFileFolders.FilesAbsPath, _settingsFilename));
        }

        [Test]
        public void CryptoKey()
        {
            const string newKey = "some-random-key-for-testing";
            var oldValue = Settings.CryptoKey;
            Settings.CryptoKey = newKey;
            Assert.AreEqual(newKey, Settings.CryptoKey);
            Settings.CryptoKey = oldValue;
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void Settings_Save_and_Restore(bool cryptoEnabled)
        {
            Settings.CryptoEnabled = cryptoEnabled;
            var outMs = new MemoryStream();
            
            _outSettings.Serialize(outMs, Encoding.UTF8);
            var inSettings = Settings.Deserialize(outMs, Encoding.UTF8);

            Assert.IsTrue(inSettings.SenderConfig.Equals(_outSettings.SenderConfig));
            outMs.Dispose();

            var smtpCredential = (Credential) _outSettings.SenderConfig.SmtpClientConfig.First().NetworkCredential;
            Assert.AreEqual(cryptoEnabled, smtpCredential.Password != smtpCredential.PasswordEncrypted && smtpCredential.Username != smtpCredential.UsernameEncrypted);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void Settings_Save_and_Restore_With_String(bool cryptoEnabled)
        {
            Settings.CryptoEnabled = cryptoEnabled;

            var serialized = _outSettings.Serialize();
            var restored = Settings.Deserialize(serialized);

            Assert.IsTrue(restored.SenderConfig.Equals(_outSettings.SenderConfig));

            var smtpCredential = (Credential)_outSettings.SenderConfig.SmtpClientConfig.First().NetworkCredential;
            Assert.AreEqual(cryptoEnabled, smtpCredential.Password != smtpCredential.PasswordEncrypted && smtpCredential.Username != smtpCredential.UsernameEncrypted);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void Settings_Restore_From_File(bool cryptoEnabled)
        {
            Settings.CryptoEnabled = cryptoEnabled;
            if (!cryptoEnabled)
            {
                var restored =
                    Settings.Deserialize(Path.Combine(TestFileFolders.FilesAbsPath, _settingsFilename), null);
                Assert.IsTrue(restored.SenderConfig.Equals(_outSettings.SenderConfig));
            }
            else
            {
                // An exception is thrown because username / password are saved as plain text,
                // while with encryption enabled, both should be encrypted.
                Assert.Throws<YAXLib.YAXPropertyCannotBeAssignedTo>(() =>
                    Settings.Deserialize(Path.Combine(TestFileFolders.FilesAbsPath, _settingsFilename), null));
            }
        }
    }
}
