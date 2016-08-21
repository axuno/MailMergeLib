using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MailMergeLib
{
	/// <summary>
	/// Configuration for MailMergeSender.
	/// </summary>
	public class SenderConfig
	{
		private SmtpClientConfig[] _smtpClientConfig = { new SmtpClientConfig() };

		/// <summary>
		/// CTOR for MailMergeSender configuration.
		/// </summary>
		public SenderConfig()
		{}
		
		/// <summary>
		/// Gets or sets the array of configurations the SmtpClients will use.
		/// The first SmtpClientConfig is the "standard", any second is the "backup".
		/// Other instances of SmtpClientConfig in the array are used for parallel sending messages.
		/// </summary>
		[XmlArray("SmtpClients")]
		[XmlArrayItem(ElementName = "SmtpClient")]
		public SmtpClientConfig[] SmtpClientConfig
		{
			get { return _smtpClientConfig; }
			set { _smtpClientConfig = value ?? new [] { new SmtpClientConfig() }; }
		}
	}
}
