using System.IO;
using MailMergeLib.Serialization;

namespace MailMergeLib.MessageStore
{
    /// <summary>
    /// A class for storing metadata about a <see cref="MailMergeMessage"/>, when
    /// serialized <see cref="MailMergeMessage"/>es are stored in the file system.
    /// </summary>
    public class FileMessageInfo : MessageInfoBase
    {
        /// <summary>
        /// The location of the xml serialized cref="MailMergeMessage"/> file.
        /// </summary>
        public FileInfo MessageFile { get; set; }

        /// <summary>
        /// Deserializes the <see cref="MailMergeMessage"/> and returns a new message object.
        /// </summary>
        /// <returns>Returns the deserialized object from the <see cref="MessageFile"/>.</returns>
        public override MailMergeMessage LoadMessage()
        {
            var serializer = SerializationFactory.GetStandardSerializer(typeof(MailMergeMessage));
            return (MailMergeMessage) serializer.DeserializeFromFile(MessageFile.FullName);
        }
    }
}
