using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Xml.Serialization;

namespace MailMergeLib
{
	/// <summary>
	/// MailMergeLib settings
	/// </summary>
	[XmlRoot(Namespace = "http://www.axuno.net/MailMergeLib/XmlSchema/5.0", IsNullable = true, ElementName = "MailMergeLibSettings")]
	public class Settings
	{
		/// <summary>
		/// CTOR
		/// </summary>
		public Settings()
		{
			SmtpClientConfig = new SmtpClientConfig[1];
		}

		/// <summary>
		/// Gets or sets a configuration list of type SmtpClientConfig.
		/// </summary>
		[XmlArrayItem(ElementName = "SmtpClient")]
		public SmtpClientConfig[] SmtpClientConfig;

		/// <summary>
		/// Write MailMergeLib settings to a file.
		/// </summary>
		/// <param name="filename"></param>
		public void Serialize(string filename)
		{
			var xmlNamespace = new XmlSerializerNamespaces();
			xmlNamespace.Add(string.Empty, "http://www.axuno.net/MailMergeLib/XmlSchema/5.0");

			var serializer = new XmlSerializer(typeof(Settings), GetXmlOverrides());
			var writer = new StreamWriter(filename);
			serializer.Serialize(writer, this);
			writer.Close();
		}

		/// <summary>
		/// Reads MailMergeLib settings from a file.
		/// </summary>
		/// <param name="filename"></param>
		/// <returns>Returns a MailMergeLib Settings instance</returns>
		public static Settings Deserialize(string filename)
		{
			var serializer = new XmlSerializer(typeof(Settings), GetXmlOverrides());
			var reader = new StreamReader(filename);
			var s = serializer.Deserialize(reader) as Settings;
			return s;
		}

		private static XmlAttributeOverrides GetXmlOverrides()
		{
			var attributeOverrides = new XmlAttributeOverrides();

			// do not serialize the SecurePassword member of NetworkCredential
			attributeOverrides.Add(typeof(NetworkCredential), nameof(NetworkCredential.SecurePassword), new XmlAttributes { XmlIgnore = true });

			// use properties as attributes instead of element
			attributeOverrides.Add(typeof(NetworkCredential), nameof(NetworkCredential.UserName), new XmlAttributes { XmlAttribute = new XmlAttributeAttribute() });
			attributeOverrides.Add(typeof(NetworkCredential), nameof(NetworkCredential.Password), new XmlAttributes { XmlAttribute = new XmlAttributeAttribute() });
			attributeOverrides.Add(typeof(NetworkCredential), nameof(NetworkCredential.Domain), new XmlAttributes { XmlAttribute = new XmlAttributeAttribute() });

			return attributeOverrides;
		}
	}
}
