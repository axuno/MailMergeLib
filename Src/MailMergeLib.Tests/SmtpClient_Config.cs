using System.Net.Mail;
using NUnit.Framework;
#if NETFRAMEWORK
using System.Configuration;
using System.Net.Configuration;
#endif

namespace MailMergeLib.Tests
{
    [TestFixture]
    public class SmtpClient_Config
    {
#if NETFRAMEWORK
        [Test]
        [TestCase(SmtpDeliveryMethod.Network, false)]
        [TestCase(SmtpDeliveryMethod.PickupDirectoryFromIis, false)]
        [TestCase(SmtpDeliveryMethod.SpecifiedPickupDirectory, false)]
        [TestCase(SmtpDeliveryMethod.Network, true)]
        [TestCase(SmtpDeliveryMethod.PickupDirectoryFromIis, true)]
        [TestCase(SmtpDeliveryMethod.SpecifiedPickupDirectory, true)]
        //[Ignore("Read_SmtpConfig_From_ConfigFile")]
        public void Read_SmtpConfig_From_ConfigFile(SmtpDeliveryMethod smtpDeliveryMethod, bool enableSsl)
        {
            var smtpConfig = new SmtpClientConfig();
            var smtpSettings = new SmtpSection()
            {
                DeliveryMethod = smtpDeliveryMethod,
                Network = {EnableSsl = enableSsl}
            };
            ChangeSmtpConfigFile(smtpSettings);
            smtpConfig.ReadSmtpConfigurationFromConfigFile();

            Assert.AreEqual(smtpDeliveryMethod == SmtpDeliveryMethod.Network ? MessageOutput.SmtpServer :
                smtpDeliveryMethod == SmtpDeliveryMethod.PickupDirectoryFromIis ? MessageOutput.PickupDirectoryFromIis :
                smtpDeliveryMethod == SmtpDeliveryMethod.SpecifiedPickupDirectory ? MessageOutput.Directory :
                MessageOutput.None, smtpConfig.MessageOutput);

            Assert.AreEqual(enableSsl ? MailKit.Security.SecureSocketOptions.Auto : MailKit.Security.SecureSocketOptions.None, smtpConfig.SecureSocketOptions);
        }

        [Test]
        [TestCase("user", "password", true)]
        [TestCase("", "password", false)]
        [TestCase("user", "", false)]
        [TestCase(null, null, false)]
        public void Read_SmtpConfig_From_ConfigFile(string username, string password, bool credentialSet)
        {
            var smtpConfig = new SmtpClientConfig();
            var smtpSettings = new SmtpSection()
            {
                Network = { UserName = username, Password = password}
            };
            ChangeSmtpConfigFile(smtpSettings);
            smtpConfig.ReadSmtpConfigurationFromConfigFile();

            Assert.AreEqual(credentialSet, smtpConfig.NetworkCredential != null);
            if (credentialSet)
            {
                Assert.AreEqual(username, ((Credential?) smtpConfig.NetworkCredential)?.Username);
                Assert.AreEqual(password, ((Credential?) smtpConfig.NetworkCredential)?.Password);
            }
        }

        [Test]
        [TestCase("host", 25, "domain")]
        [TestCase(null, 123, null)]
        public void Read_SmtpConfig_From_ConfigFile(string host, int port, string clientDomain)
        {
            var smtpConfig = new SmtpClientConfig();
            var smtpSettings = new SmtpSection()
            {
                Network = { Host = host, Port = port, ClientDomain = clientDomain}
            };
            ChangeSmtpConfigFile(smtpSettings);
            smtpConfig.ReadSmtpConfigurationFromConfigFile();

            Assert.IsTrue(smtpConfig.SmtpHost == host && smtpConfig.SmtpPort == port && smtpConfig.ClientDomain == clientDomain);
        }

        /// <summary>
        /// Changes the .config file of the app.
        /// Neither the .config must exist, nor the mailSettings have to be there.
        /// If the app doesn't have a .config, it will be created.
        /// </summary>
        /// <param name="newSmtpSettings">The SMTP settings to write to the .config file.</param>
        private static void ChangeSmtpConfigFile(SmtpSection newSmtpSettings)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.SectionGroups["system.net"]?.SectionGroups["mailSettings"]?.Sections.Clear();
            var smtpSettings = (SmtpSection)config.GetSection("system.net/mailSettings/smtp");

            smtpSettings.DeliveryMethod = newSmtpSettings.DeliveryMethod;
            smtpSettings.DeliveryFormat = newSmtpSettings.DeliveryFormat;
            smtpSettings.Network.EnableSsl = newSmtpSettings.Network.EnableSsl;
            smtpSettings.Network.UserName = newSmtpSettings.Network.UserName;
            smtpSettings.Network.Password = newSmtpSettings.Network.Password;
            smtpSettings.Network.ClientDomain = newSmtpSettings.Network.ClientDomain;
            smtpSettings.Network.Host = newSmtpSettings.Network.Host;
            smtpSettings.Network.Port = newSmtpSettings.Network.Port;

            config.Save(ConfigurationSaveMode.Minimal, true);
            ConfigurationManager.RefreshSection("system.net");
        }

        [Test]
        public void Message_Output_PickupDirectoryFromIis()
        {
            // var smtpConfig = new SmtpClientConfig
            // {
            //    MessageOutput = MessageOutput.PickupDirectoryFromIis
            // };
            // May throw for many reasons:
            // https://torontoprogrammer.ca/2011/04/fixing-the-cannot-get-iis-pickup-directory-error-in-asp-net/
            // Assert.DoesNotThrow(() => { var x = smtpConfig.MailOutputDirectory; });
        }
#endif
        [Test]
        public void Message_Output_Directory()
        {
            var smtpConfig = new SmtpClientConfig
            {
                MessageOutput = MessageOutput.Directory
            };
            Assert.AreEqual(System.IO.Path.GetTempPath(), smtpConfig.MailOutputDirectory);
        }

        [Test]
        public void Equality()
        {
            var sc1 = new SmtpClientConfig();
            var sc2 = new SmtpClientConfig();

            Assert.IsTrue(sc1.Equals(sc2));
            Assert.IsFalse(sc1.Equals(new object()));
        }

        [Test]
        public void NotEqual()
        {
            var sc1 = new SmtpClientConfig();
            var sc2 = new SmtpClientConfig { SmtpPort = 12345 };

            Assert.IsFalse(sc1.Equals(sc2));
            Assert.IsFalse(sc1.Equals(new object()));
        }
    }
}
