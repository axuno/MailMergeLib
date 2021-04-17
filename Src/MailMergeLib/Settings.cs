using System.IO;
using System.Text;
using MailMergeLib.Serialization;
using YAXLib;

namespace MailMergeLib
{
    /// <summary>
    /// MailMergeLib settings
    /// </summary>
    [YAXSerializableType(FieldsToSerialize = YAXSerializationFields.AttributedFieldsOnly)]
    [YAXSerializeAs("MailMergeLibSettings")]
    public class Settings
    {
        /// <summary>
        /// CTOR
        /// </summary>
        public Settings()
        {
            SenderConfig = new SenderConfig();
            MessageConfig = new MessageConfig();
        }

        /// <summary>
        /// Configuration for the MailMergeSender.
        /// </summary>
        [YAXSerializableField]
        [YAXSerializeAs("Sender")]
        public SenderConfig SenderConfig { get; set; }

        /// <summary>
        /// Configuration for the MailMergeMessage.
        /// </summary>
        [YAXSerializableField]
        [YAXSerializeAs("Message")]
        public MessageConfig MessageConfig { get; set; }

        /// <summary>
        /// Gets or sets the static <see cref="Crypto.CryptoKey"/> used for encryption and decryption.
        /// Should be set individually.
        /// </summary>
        public static string CryptoKey
        {
            get => Crypto.CryptoKey;
            set => Crypto.CryptoKey = value;
        }

        /// <summary>
        /// Static switch to set encryption for settings on or off. Default is <c>false</c>.
        /// </summary>
        [YAXDontSerialize]
        public static bool CryptoEnabled { get; set; } = false;

        /// <summary>
        /// Write MailMergeLib settings to a stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="encoding"></param>
        public void Serialize(Stream stream, Encoding encoding)
        {
            Serialize(new StreamWriter(stream, encoding), true);
        }

        /// <summary>
        /// Write MailMergeLib settings to a file.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="encoding"></param>
        public void Serialize(string filename, Encoding encoding = null)
        {
            using var fs = new FileStream(filename, FileMode.Create);
            using var sr = new StreamWriter(fs, encoding ?? Encoding.UTF8);
            Serialize(sr, false);
        }

        /// <summary>
        /// Get MailMergeLib settings as an xml string.
        /// </summary>
        /// <returns>Returns the MailMergeLib settings as an xml string.</returns>
        public string Serialize()
        {
            return SerializationFactory.Serialize(this);
        }

        /// <summary>
        /// Write MailMergeLib settings with a StreamWriter.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="isStream">If true, the writer will not be closed and disposed, so that the underlying stream can be used on return.</param>
        private void Serialize(TextWriter writer, bool isStream)
        {
            SerializationFactory.Serialize(this, writer, isStream);
        }

        /// <summary>
        /// Reads MailMergeLib settings from a stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="encoding"></param>
        public static Settings Deserialize(Stream stream, Encoding encoding)
        {
            using var sr = new StreamReader(stream, encoding);
            return SerializationFactory.Deserialize<Settings>(sr, true);
        }

        /// <summary>
        /// Reads MailMergeLib settings from a file.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="encoding"></param>
        public static Settings Deserialize(string filename, Encoding encoding)
        {
            return SerializationFactory.Deserialize<Settings>(filename, encoding ?? Encoding.UTF8);
        }

        /// <summary>
        /// Read the MailMergeLib settings from an xml string.
        /// </summary>
        /// <returns>Returns the MailMergeLib settings as an xml string.</returns>
        public static Settings Deserialize(string xml)
        {
            return SerializationFactory.Deserialize<Settings>(xml);
        }
    }
}
