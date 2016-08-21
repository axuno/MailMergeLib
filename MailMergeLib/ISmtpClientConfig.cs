using System.Net;
using System.Xml.Serialization;
using MailKit;
using MailKit.Security;

namespace MailMergeLib
{
	/// <summary>
	/// Interface for the smtp client configuration.
	/// </summary>
	public interface ISmtpClientConfig
	{
		/// <summary>
		/// Get or sets the name of configuration.
		/// It's recommended to choose different names for each configuration.
		/// </summary>
		string Name { get; set; }

		/// <summary>
		/// Gets or sets the name or IP address of the SMTP host to be used for sending mails.
		/// </summary>
		string SmtpHost { get; set; }

		/// <summary>
		/// Gets or set the port of the SMTP host to be used for sending mails.
		/// </summary>
		int SmtpPort { get; set; }

		/// <summary>
		/// Gets or sets the name of the local machine sent to the SMTP server with the hello command
		/// of an SMTP transaction. Defaults to the windows machine name.
		/// </summary>
		string ClientDomain { get; set; }

		/// <summary>
		/// Gets or sets the local IP end point or null to use the default end point.
		/// </summary>
		IPEndPoint LocalEndPoint { get; set; }

		/// <summary>
		/// Verifies the remote Secure Sockets Layer (SSL) certificate used for authentication.
		/// </summary>
		System.Net.Security.RemoteCertificateValidationCallback ServerCertificateValidationCallback { get; set; }

		/// <summary>
		/// Set authentification details for logging into an SMTP server.
		/// Set NetworkCredential to null if no authentification is required.
		/// </summary>
		Credential NetworkCredential { get; set; }

		/// <summary>
		/// Gets or sets the name of the output directory of sent mail messages
		/// (only used if messages are not sent to SMTP server)
		/// </summary>
		string MailOutputDirectory { get; set; }

		/// <summary>
		/// Gets or sets the location where to send mail messages.
		/// </summary>
		MessageOutput MessageOutput { get; set; }

		/// <summary>
		/// Gets or sets the SecureSocketOptions the SmtpClient will use (e.g. SSL or STARTLS
		/// In case a secure socket is needed, setting options to SecureSocketOptions.Auto is recommended.
		/// </summary>
		SecureSocketOptions SecureSocketOptions { get; set; }

		/// <summary>
		/// Gets or sets the timeout for sending a message, after which a time-out exception will raise.
		/// Time-out value in milliseconds. The default value is 100,000 (100 seconds). 
		/// </summary>
		int Timeout { get; set; }

		/// <summary>
		/// Gets or sets the IProtocolLogger the SmtpClient will use to log the dialogue with the SMTP server.
		/// Set ProtocolLooger to null for no logging.
		/// </summary>
		/// <remarks>
		/// Have in mind that MailMergeLib may use several SmtpClients concurrently.
		/// </remarks>
		IProtocolLogger GetProtocolLogger();

		/// <summary>
		/// Gets or sets the directory where ProtocolLogger will write its logs
		/// </summary>
		string LogOutputDirectory { get; set; }

		/// <summary>
		/// If true, ProcolLogger is enabled.
		/// </summary>
		bool EnableLogOutput { get; set; }

		/// <summary>
		/// Gets or sets the delay time in milliseconds (0-10000) between the messages.
		/// In case more than one SmtpClient will be used concurrently, the delay will be used per thread.
		/// Mainly used for debug purposes.
		/// </summary>
		int DelayBetweenMessages { get; set; }

		/// <summary>
		/// Gets or sets the number of failures (1-5) for which a retry to send will be performed by SmtpClient.
		/// </summary>
		int MaxFailures { get; set; }

		/// <summary>
		/// Gets or sets the delay time in milliseconds (0-10000) to elaps between retries to send the message.
		/// </summary>
		int RetryDelayTime { get; set; }
	}
}