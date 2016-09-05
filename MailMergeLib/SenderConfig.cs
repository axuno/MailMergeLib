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
		private int _maxNumOfSmtpClients = 5;

		/// <summary>
		/// CTOR for MailMergeSender configuration.
		/// </summary>
		public SenderConfig()
		{}

		/// <summary>
		/// Gets or sets the maximum number of SmtpClient to send messages concurrently.
		/// Valid numbers are 1 to 20, defaults to 5.
		/// </summary>
		public int MaxNumOfSmtpClients
		{
			get { return _maxNumOfSmtpClients; }
			set { _maxNumOfSmtpClients = value > 0 && value <= 20 ? value : 5; }
		}

		/// <summary>
		/// Gets or sets the array of configurations the SmtpClients will use.
		/// The first SmtpClientConfig is the "standard", any second is the "backup".
		/// Other instances of SmtpClientConfig in the array are used for parallel sending messages.
		/// </summary>
		[XmlArray("SmtpClients")]
		[XmlArrayItem(ElementName = "SmtpClient")]
		public SmtpClientConfig[] SmtpClientConfig { get; set; } = {new SmtpClientConfig()};
	}
}
