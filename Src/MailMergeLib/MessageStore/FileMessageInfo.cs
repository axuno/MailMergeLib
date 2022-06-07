using System.IO;
using System.Text;
using MailMergeLib.Serialization;

namespace MailMergeLib.MessageStore;

/// <summary>
/// A class for storing metadata about a <see cref="MailMergeMessage"/>, when
/// serialized <see cref="MailMergeMessage"/>es are stored in the file system.
/// </summary>
public class FileMessageInfo : MessageInfoBase
{
    /// <summary>
    /// CTOR.
    /// </summary>
    internal FileMessageInfo()
    {}

    /// <summary>
    /// The location of the xml serialized <see cref="MailMergeMessage"/> file.
    /// </summary>
    public FileInfo MessageFile { get; internal set; }

    /// <summary>
    /// Gets or sets the <see cref="Encoding"/> to apply when loading <see cref="MailMergeMessage"/>s from the file system.
    /// </summary>
    internal Encoding MessageEncoding { get; set; } = Encoding.UTF8; // set by FileMessageStore

    /// <summary>
    /// Deserializes the <see cref="MailMergeMessage"/> and returns a new message object.
    /// </summary>
    /// <returns>Returns the deserialized object from the <see cref="MessageFile"/>.</returns>
    public override MailMergeMessage LoadMessage()
    {
        return SerializationFactory.Deserialize<MailMergeMessage>(MessageFile.FullName, MessageEncoding);
    }
}