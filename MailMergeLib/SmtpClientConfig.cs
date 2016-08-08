using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Configuration;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MailKit;
using MailKit.Security;


namespace MailMergeLib
{
	/// <summary>
	/// Enumeration of message output types
	/// </summary>
	public enum MessageOutput
	{
		/// <summary>
		/// Will process all messages but discard them just before sending / writing to disk.
		/// </summary>
		None,
		/// <summary>
		/// Send messages through an SMTP server
		/// </summary>
		SmtpServer,
		/// <summary>
		/// Writes messages to the specified MailOutputDirectory.
		/// </summary>
		Directory,
		/// <summary>
		/// Think twice about using the option &quot;IIS Pickup Directory&quot;. Then make sure that:
		/// 1. SMTP is installed
		/// 2. SMTP is configured
		/// 3. Firewall is open
		/// 4. IIS has access to the metabase
		/// 5. IIS has access to the pickup directory
		/// Otherwise you'll expect an SmtpException while method GetPickDirectoryFromIis() is called 
		/// </summary>
		PickupDirectoryFromIis
	}


	/// <summary>
	/// Class which is used by MailMergeSender in order to build a preconfigured SmtpClient
	/// </summary>
	public class SmtpClientConfig
	{

		private ICredentials _credentials;

		/// <summary>
		/// Creates a new instance of the configuration which is used by MailMergeSender in order to build a preconfigured SmtpClient.
		/// </summary>
		public SmtpClientConfig()
		{
		}

		/// <summary>
		/// Creates a new instance of the configuration which is used by MailMergeSender in order to build a preconfigured SmtpClient.
		/// </summary>
		/// <param name="readDefaultsFromConfigFile">If true, the configurations is read from system.net/mailSettings/smtp configuration section.</param>
		public SmtpClientConfig(bool readDefaultsFromConfigFile = true)
		{
			if (!readDefaultsFromConfigFile)
			{
				return;
			}

			var smtpSection = ConfigurationManager.GetSection("system.net/mailSettings/smtp") as SmtpSection;
			if (smtpSection == null) return;

			switch (smtpSection.DeliveryMethod)
			{
				case System.Net.Mail.SmtpDeliveryMethod.Network:
					MessageOutput = MessageOutput.SmtpServer;
					break;
				case System.Net.Mail.SmtpDeliveryMethod.PickupDirectoryFromIis:
					MessageOutput = MessageOutput.PickupDirectoryFromIis;
					MailOutputDirectory = GetPickDirectoryFromIis();
					break;
				case System.Net.Mail.SmtpDeliveryMethod.SpecifiedPickupDirectory:
					MessageOutput = MessageOutput.Directory;
					MailOutputDirectory = smtpSection.SpecifiedPickupDirectory.PickupDirectoryLocation;
					break;
			}

			if (smtpSection.Network.EnableSsl) SecureSocketOptions = SecureSocketOptions.Auto;

			Credentials = new NetworkCredential(smtpSection.Network.UserName, smtpSection.Network.Password);

			SmtpPort = smtpSection.Network.Port;
			SmtpHost = smtpSection.Network.Host;
		}	
		
		/// <summary>
		/// Gets or sets the name or IP address of the SMTP host to be used for sending mails.
		/// </summary>
		public string SmtpHost { get; set; } = "localhost";

		/// <summary>
		/// Gets or set the port of the SMTP host to be used for sending mails.
		/// </summary>
		public int SmtpPort { get; set; } = 25;

		/// <summary>
		/// Gets or sets the name of the local machine sent to the SMTP server in the hello command
		/// of an SMTP transaction. Defaults to the windows machine name.
		/// </summary>
		public string LocalHostName { get; set; }

		/// <summary>
		/// Gets or sets the name of the output directory of sent mail messages
		/// (only used if messages are not sent to SMTP server)
		/// </summary>
		public string MailOutputDirectory { get; set; } = null;

		/// <summary>
		/// Gets or sets the location where to send mail messages.
		/// </summary>
		public MessageOutput MessageOutput { get; set; } = MessageOutput.None;

		/// <summary>
		/// Gets or sets the SecureSocketOptions the SmtpClient will use (e.g. SSL or STARTLS
		/// In case a secure socket is needed, setting options to SecureSocketOptions.Auto is recommended.
		/// </summary>
		public SecureSocketOptions SecureSocketOptions { get; set; } = SecureSocketOptions.None;

		/// <summary>
		/// Gets or sets the timeout for sending a message, after which a time-out exception will raise.
		/// Time-out value in milliseconds. The default value is 100,000 (100 seconds). 
		/// </summary>
		public int Timeout { get; set; } = 100000;


		/// <summary>
		/// Set authentification details for logging into an SMTP server.
		/// Set Credentials to null if no authentification is required.
		/// </summary>
		/// <example>Credentials = new NetworkCredential(username, password)</example>
		public ICredentials Credentials { get; set; }


		/// <summary>
		/// Gets or sets the IProtocolLogger the SmtpClient will use to log the dialogue with the SMTP server.
		/// Set ProtocolLooger to null for no logging.
		/// </summary>
		/// <remarks>
		/// Have in mind that MailMergeLib may use several SmtpClients concurrently.
		/// </remarks>
		public IProtocolLogger ProtocolLogger { get; set; }

		private static string GetPickDirectoryFromIis()
		{
			try
			{
				var internalType = typeof(System.Net.Mail.SmtpClient).Assembly.GetType("System.Net.Mail.IisPickupDirectory");
				if (internalType == null)
				{
					throw new NotImplementedException("Assembly does not contain type System.Net.Mail.IisPickupDirectory");
				}
				var smtpClient = new System.Net.Mail.SmtpClient();

				var methodInfo = internalType.GetMethod("GetPickupDirectory",
					BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
				if (methodInfo == null)
				{
					throw new NotImplementedException(
						"System.Net.Mail.IisPickupDirectory does not contain a method GetPickupDirectory()");
				}
				var obj = methodInfo.Invoke(smtpClient, null);

				return obj as string;
			}
			catch (TargetInvocationException ex)
			{
				// most likely an SmtpException will throw
				throw ex.InnerException ?? ex;
			}

		}
	}
}
