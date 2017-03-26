using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Text;
using MailMergeLib;
using MailMergeLib.SmartFormatMail.Core.Settings;
using MimeKit;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class ConfigTests
    {
        [Test]
        public void SaveAndReadSettings()
        {
            Settings.CryptoKey = "SecretCryptoKey";
            var outSettings = new Settings
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
                            SmtpHost = "some.host.com",
                            SmtpPort = 123,
                            NetworkCredential = new Credential("user", "password"),
                            Name = "Best",
                            MaxFailures = 10,
                            DelayBetweenMessages = 543
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
            
            var outMs = new MemoryStream();
            outSettings.Serialize(outMs, Encoding.UTF8);

            outMs.Seek(0, SeekOrigin.Begin);
            var inSettings = Settings.Deserialize(outMs, Encoding.UTF8);

            var inMs = new MemoryStream();
            inSettings.Serialize(inMs, Encoding.UTF8);

            Assert.True(Compare(outMs, inMs) == 0);
        }

        private static int Compare(Stream a, Stream b)
        {
            if (a == null && b == null) return 0;

            if (a == null || b == null) throw new ArgumentNullException(a == null ? "a" : "b");

            if (a.Length < b.Length) return -1;

            if (a.Length > b.Length) return 1;

            int buf;
            while ((buf = a.ReadByte()) != -1)
            {
                var diff = buf.CompareTo(b.ReadByte());
                if (diff != 0) return diff;
            }
            return 0;
        }

    }
}
