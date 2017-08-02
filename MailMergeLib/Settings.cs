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
        /// Gets or sets the Key used for encryption and decryption.
        /// Should be set individually.
        /// </summary>
        public static string CryptoKey
        {
            get { return Crypto.CryptoKey; }
            set { Crypto.CryptoKey = value; }
        }
        
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
        public void Serialize(string filename)
        {
            using (var fs = new FileStream(filename, FileMode.Create))
            {
                using (var sr = new StreamWriter(fs))
                {
                    Serialize(sr, false);
                }
            }
        }

        /// <summary>
        /// Get MailMergeLib settings as an xml string.
        /// </summary>
        /// <returns>Returns the MailMergeLib settings as an xml string.</returns>
        public string Serialize()
        {
            return SerializationFactory.GetStandardSerializer(typeof(Settings)).Serialize(this);
        }

        /// <summary>
        /// Write MailMergeLib settings with a StreamWriter.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="isStream">If true, the writer will not be closed and disposed, so that the underlying stream can be used on return.</param>
        private void Serialize(TextWriter writer, bool isStream)
        {
            var serializer = SerializationFactory.GetStandardSerializer(typeof(Settings));
            serializer.Serialize(this, writer);
            writer.Flush();

            if (isStream) return;

#if NET40 || NET45
            writer.Close();
#endif
            writer.Dispose();
        }

        /// <summary>
        /// Reads MailMergeLib settings from a stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="encoding"></param>
        public static Settings Deserialize(Stream stream, Encoding encoding)
        {
            return Deserialize(new StreamReader(stream, encoding), true);
        }

        /// <summary>
        /// Reads MailMergeLib settings from a file.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="encoding"></param>
        public static Settings Deserialize(string filename, Encoding encoding)
        {
            using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var sr = new StreamReader(fs, encoding))
                {
                    return Deserialize(sr, false);
                }
            }
        }

        /// <summary>
        /// Read the MailMergeLib settings from an xml string.
        /// </summary>
        /// <returns>Returns the MailMergeLib settings as an xml string.</returns>
        public static Settings Deserialize(string xml)
        {
            return (Settings) SerializationFactory.GetStandardSerializer(typeof(Settings)).Deserialize(xml);
        }

        /// <summary>
        /// Reads MailMergeLib settings with a StreamReader.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>Returns a MailMergeLib Settings instance</returns>
        /// <param name="isStream">If true, the writer will not be closed and disposed, so that the underlying stream can be used on return.</param>
        private static Settings Deserialize(StreamReader reader, bool isStream)
        {
            var serializer = SerializationFactory.GetStandardSerializer(typeof(Settings));
            reader.BaseStream.Position = 0;
            var str = reader.ReadToEnd();
            var s = (Settings)serializer.Deserialize(str);
  
            if (isStream) return s;
#if NET40 || NET45
            reader.Close();
#endif
            reader.Dispose();

            return s;
        }
    }
}
