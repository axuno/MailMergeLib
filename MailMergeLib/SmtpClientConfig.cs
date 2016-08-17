using System;
using System.Configuration;
using System.Net;
using System.Net.Configuration;
using System.Reflection;
using System.Xml.Serialization;
using MailKit;
using MailKit.Security;


namespace MailMergeLib
{
	/// <summary>
	/// Class which is used by MailMergeSender in order to build preconfigured SmtpClients.
	/// </summary>
	public class SmtpClientConfig : ISmtpClientConfig
	{
		private int _maxFailures = 1;
		private int _retryDelayTime;
		private string _mailOutputDirectory = null;

		/// <summary>
		/// Creates a new instance of the configuration which is used by MailMergeSender in order to build a preconfigured SmtpClient.
		/// </summary>
		public SmtpClientConfig()
		{
			MailOutputDirectory = LogOutputDirectory = System.IO.Path.GetTempPath();
		}


		/// <summary>
		/// If MailMergeLib runs on an IIS web application, it can load the following settings from system.net/mailSettings/smtp configuration section of web.donfig:
		/// DeliveryMethod, MessageOutput, EnableSsl, Network.UserName, Network.Password, Network.Host, Network.Port, Network.ClientDomain
		/// </summary>
		public void SmtpConfigurationFromWebConfig()
		{
			var smtpSection = ConfigurationManager.GetSection("system.net/mailSettings/smtp") as SmtpSection;
			if (smtpSection == null) return;

			switch (smtpSection.DeliveryMethod)
			{
				case System.Net.Mail.SmtpDeliveryMethod.Network:
					MessageOutput = MessageOutput.SmtpServer;
					break;
				case System.Net.Mail.SmtpDeliveryMethod.PickupDirectoryFromIis:
					MessageOutput = MessageOutput.PickupDirectoryFromIis;
					break;
				case System.Net.Mail.SmtpDeliveryMethod.SpecifiedPickupDirectory:
					MessageOutput = MessageOutput.Directory;
					break;
			}

			if (smtpSection.Network.EnableSsl) SecureSocketOptions = SecureSocketOptions.Auto;

			if (!string.IsNullOrEmpty(smtpSection.Network.UserName) && !string.IsNullOrEmpty(smtpSection.Network.Password))
				NetworkCredential = new NetworkCredential(smtpSection.Network.UserName, smtpSection.Network.Password);
			
			SmtpPort = smtpSection.Network.Port;
			SmtpHost = smtpSection.Network.Host;
			ClientDomain = smtpSection.Network.ClientDomain;
		}

		/// <summary>
		/// Get or sets the name of configuration.
		/// It's recommended to choose different names for each configuration.
		/// </summary>
		[XmlAttribute]
		public string Name { get; set; }


		/// <summary>
		/// Gets or sets the name or IP address of the SMTP host to be used for sending mails.
		/// </summary>
		/// <remarks>Used during SmtpClient connect.</remarks>
		public string SmtpHost { get; set; } = "localhost";

		/// <summary>
		/// Gets or set the port of the SMTP host to be used for sending mails.
		/// </summary>
		/// <remarks>Used during SmtpClient connect.</remarks>
		public int SmtpPort { get; set; } = 25;

		/// <summary>
		/// Gets or sets the name of the local machine sent to the SMTP server with the hello command
		/// of an SMTP transaction. Defaults to the windows machine name.
		/// </summary>
		public string ClientDomain { get; set; }


		/// <summary>
		/// Gets or sets the local IP end point or null to use the default end point.
		/// </summary>
		[XmlIgnore]
		public IPEndPoint LocalEndPoint { get; set; }

		/// <summary>
		/// Verifies the remote Secure Sockets Layer (SSL) certificate used for authentication.
		/// </summary>
		[XmlIgnore]
		public System.Net.Security.RemoteCertificateValidationCallback ServerCertificateValidationCallback { get; set; }

		/// <summary>
		/// Set authentification details for logging into an SMTP server.
		/// Set NetworkCredential to null if no authentification is required.
		/// </summary>
		/// <remarks> Used during SmtpClient connect.</remarks>
		public NetworkCredential NetworkCredential { get; set; }

		/// <summary>
		/// Gets or sets the name of the output directory of sent mail messages
		/// (only used if messages are not sent to SMTP server)
		/// </summary>
		public string MailOutputDirectory
		{
			get
			{
				switch (MessageOutput)
				{
					case MessageOutput.None:
					case MessageOutput.SmtpServer:
						return null;
					case MessageOutput.Directory:
						return _mailOutputDirectory ?? System.IO.Path.GetTempPath();
					case MessageOutput.PickupDirectoryFromIis:
						return GetPickDirectoryFromIis();
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			set { _mailOutputDirectory = value; }
		}

		/// <summary>
		/// Gets or sets the location where to send mail messages.
		/// </summary>
		public MessageOutput MessageOutput { get; set; } = MessageOutput.None;

		/// <summary>
		/// Gets or sets the SecureSocketOptions the SmtpClient will use (e.g. SSL or STARTLS
		/// In case a secure socket is needed, setting options to SecureSocketOptions.Auto is recommended.
		/// </summary>
		/// <remarks>Used during SmtpClient connect.</remarks>
		public SecureSocketOptions SecureSocketOptions { get; set; } = SecureSocketOptions.None;

		/// <summary>
		/// Gets or sets the timeout for sending a message, after which a time-out exception will raise.
		/// Time-out value in milliseconds. The default value is 100,000 (100 seconds). 
		/// </summary>
		public int Timeout { get; set; } = 100000;


		/// <summary>
		/// Gets the IProtocolLogger the SmtpClient will use to log the dialogue with the SMTP server.
		/// </summary>
		/// <remarks>
		/// Have in mind that MailMergeLib may use several SmtpClients concurrently.
		/// Switch logging for new SmtpClients on/off using EnableLogOutput.
		/// Used when creating a new instance of SmtpClient.
		/// </remarks>
		public IProtocolLogger GetProtocolLogger()
		{
			return new ProtocolLogger(System.IO.Path.Combine(LogOutputDirectory, "Smtp-" + System.IO.Path.GetRandomFileName()+".log"));
		}

		/// <summary>
		/// Gets or sets the directory where ProtocolLogger will write its logs.
		/// </summary>
		/// <remarks>
		/// Defaults to System.IO.Path.GetTempPath()
		/// </remarks>
		public string LogOutputDirectory { get; set; }

		/// <summary>
		/// If true, ProcolLogger is enabled.
		/// </summary>
		public bool EnableLogOutput { get; set; }

		/// <summary>
		/// Gets or sets the delay time in milliseconds (0-10000) between the messages.
		/// In case more than one SmtpClient will be used concurrently, the delay will be used per thread.
		/// Mainly used for debug purposes.
		/// </summary>
		public int DelayBetweenMessages { get; set; }

		/// <summary>
		/// Gets or sets the number of failures (1-5) for which a retry to send will be performed.
		/// </summary>
		public int MaxFailures
		{
			get { return _maxFailures; }
			set { _maxFailures = (value >= 1 && value < 5) ? value : 1; }
		}

		/// <summary>
		/// Gets or sets the delay time in milliseconds (0-10000) to elaps between retries to send the message.
		/// </summary>
		public int RetryDelayTime
		{
			get { return _retryDelayTime; }
			set { _retryDelayTime = (value >= 0 && value <= 10000) ? value : 0; }
		}

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

				var methodInfo = internalType.GetMethod("GetPickupDirectory", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
				if (methodInfo == null)
				{
					throw new NotImplementedException("System.Net.Mail.IisPickupDirectory does not contain a method GetPickupDirectory()");
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
