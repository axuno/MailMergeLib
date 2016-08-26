using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using MailMergeLib.SmartFormatMail.Core.Settings;
using MimeKit;

namespace MailMergeLib
{
	/// <summary>
	/// Configuration for MailMergeMessage.
	/// </summary>
	public class MessageConfig
	{
		/// <summary>
		/// CTOR for MailMergeMessage configuration.
		/// </summary>
		public MessageConfig()
		{}

		/// <summary>
		/// Content transfer encoding for text like HTML.
		/// </summary>
		public ContentEncoding TextTransferEncoding { get; set; } = ContentEncoding.SevenBit;
		/// <summary>
		/// Content transfer encoding for binary content like image attachments.
		/// </summary>
		public ContentEncoding BinaryTransferEncoding { get; set; } = ContentEncoding.Base64;

		/// <summary>
		/// Character encoding.
		/// Used for serialization. It is the string representation of <see cref="CharacterEncoding"/>.
		/// </summary>
		[XmlElement("CharacterEncoding")]
		public string CharacterEncodingName
		{
			get { return CharacterEncoding.WebName; }
			set { CharacterEncoding = Encoding.GetEncoding(value);}
		}

		/// <summary>
		/// Character encoding.
		/// </summary>
		[XmlIgnore]
		public Encoding CharacterEncoding { get; set; } = Encoding.UTF8;

		/// <summary>
		/// Culture information.
		/// Used for serialization. It is the string representation of <see cref="CultureInfo"/>.
		/// </summary>
		[XmlElement("CultureInfo")]
		public string CultureInfoName
		{
			get { return CultureInfo.Name; }
			set { CultureInfo = CultureInfo.GetCultureInfo(value); }
		}

		/// <summary>
		/// Culture information.
		/// </summary>
		[XmlIgnore]
		public CultureInfo CultureInfo { get; set; } = CultureInfo.InvariantCulture;

		/// <summary>
		/// Gets or sets the local base directory of HTML linked resources and other attachments.
		/// It is useful for retrieval of inline attachments (linked resources of the HTML body).
		/// </summary>
		public string FileBaseDirectory { get; set; } = Path.GetTempPath();

		/// <summary>
		/// If true, empty or illegal recipient addresses will simply be discarded.
		/// If false, an exception will be thrown.
		/// </summary>
		public bool IgnoreIllegalRecipientAddresses { get; set; } = true;

		/// <summary>
		/// The priority header for the mail message.
		/// </summary>
		public MessagePriority Priority { get; set; } = MessagePriority.Normal;

		/// <summary>
		/// The standard mailbox address which will be used as one of the "from" addresses.
		/// Used for serialization. It is the string representation of <see cref="StandardFromAddress"/>. 
		/// </summary>
		[XmlElement("StandardFromAddress")]
		public string StandardFromAddressText
		{
			get { return StandardFromAddress?.ToString(); }
			set { StandardFromAddress = !string.IsNullOrEmpty(value) ? MailboxAddress.Parse(ParserOptions.Default, value) : null; }
		}

		/// <summary>
		/// The standard mailbox address which will be used as one of the "from" addresses.
		/// </summary>
		[XmlIgnore]
		public MailboxAddress StandardFromAddress { get; set; }

		/// <summary>
		/// The organization header of a mail message.
		/// </summary>
		public string Organization { get; set; }

		/// <summary>
		/// Gets or sets the "x-mailer" header value to be used.
		/// </summary>
		public string Xmailer { get; set; }

		/// <summary>
		/// SmartFormatter configuration for parsing and formatting errors.
		/// </summary>
		public SmartFormatterConfig SmartFormatterConfig { get; set; } = new SmartFormatterConfig();
	}

	/// <summary>
	/// SmartFormatter configuration.
	/// </summary>
	public class SmartFormatterConfig
	{
		/// <summary>
		/// Behavior of the parser in case of errors.
		/// </summary>
		public ErrorAction ParseErrorAction { get; set; } = ErrorAction.ThrowError;
		/// <summary>
		/// Behavior of the formatter in case of errors.
		/// </summary>

		public ErrorAction FormatErrorAction { get; set; } = ErrorAction.Ignore;
	}
}
