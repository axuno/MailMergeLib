using System.IO;
using System.Text;
using System.Xml;

namespace MailMergeLib.MessageStore
{
    /// <summary>
    /// Metadata about the <see cref="MailMergeMessage"/>.
    /// </summary>
    public abstract class MessageInfoBase : IMessageInfo
    {
        #region *** IMessageInfo members ***

        /// <summary>
        /// The Id of the <see cref="MailMergeMessage"/>. A user-defined string without further relevance for <see cref="MailMergeLib"/>.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// The category of the <see cref="MailMergeMessage"/>. A user-defined string without further relevance for <see cref="MailMergeLib"/>.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Description for the <see cref="MailMergeMessage"/>. A user-defined string without further relevance for <see cref="MailMergeLib"/>.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Comments for the <see cref="MailMergeMessage"/>. A user-defined string without further relevance for <see cref="MailMergeLib"/>.
        /// </summary>
        public string Comments { get; set; }

        /// <summary>
        /// Data hint for the <see cref="MailMergeMessage"/>. A user-defined string without further relevance for <see cref="MailMergeLib"/>.
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// Determines whether the specified <see cref="MessageInfoBase"/> is equal to the current.
        /// </summary>
        /// <param name="other"></param>
        /// <returns>bool</returns>
        public bool Equals(IMessageInfo other)
        {
            if (other == null) return false;
            return Id == other.Id && Category == other.Category && Description == other.Description && Comments == other.Comments && Data == other.Data;
        }

        #endregion

        /// <summary>
        /// Returns the <see cref="MailMergeMessage"/> for these metadata.
        /// Method must be overridden in a derived class.
        /// </summary>
        /// <param name="encoding">The encoding to use for loading the message.</param>
        /// <returns>Returns an instance of <see cref="MailMergeMessage"/></returns>
        public abstract MailMergeMessage LoadMessage(Encoding encoding);

        #region *** Equality ***

        /// <summary>
        /// Determines whether the specified object is equal to the current.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (!(obj is IMessageInfo)) return false;
            return Equals((IMessageInfo)obj);
        }

        /// <summary>
        /// Returns the hash code for this class.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id.GetHashCode();
                hashCode = (hashCode * 397) ^ (Category != null ? Category.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Description != null ? Description.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Comments != null ? Comments.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Data != null ? Data.GetHashCode() : 0);
                return hashCode;
            }
        }

        #endregion

        #region *** Static methods for reading IMessageInfo from a serialized MailMergeMessage ***

        // Fast XML scan for descendants of the &lt;Info&gt; element of a serialized <see cref="MailMergeMessage"/>
        // to populate the <see cref="IMessageInfo"/> properties.

        /// <summary>
        /// Get the <see cref="IMessageInfo"/> of a serialized <see cref="MailMergeMessage"/>.
        /// </summary>
        /// <param name="fileSystemInfo"></param>
        /// <returns>Return the <see cref="IMessageInfo"/> of a serialized <see cref="MailMergeMessage"/></returns>
        public static IMessageInfo Read(FileSystemInfo fileSystemInfo)
        {
            using (var xmlReader = XmlReader.Create(fileSystemInfo.FullName))
            {
                return ReadInfo(xmlReader);
            }
        }

        /// <summary>
        /// Get the <see cref="IMessageInfo"/> of a serialized <see cref="MailMergeMessage"/>.
        /// </summary>
        /// <param name="xmlString">The XML string of a serialized <see cref="MailMergeMessage"/>.</param>
        /// <returns>Return the <see cref="IMessageInfo"/> of a serialized <see cref="MailMergeMessage"/></returns>
        public static IMessageInfo Read(string xmlString)
        {
            using (var stringReader = new StringReader(xmlString))
            {
                using (var xmlReader = XmlReader.Create(stringReader))
                {
                    return ReadInfo(xmlReader);
                }
            }
        }

        private static IMessageInfo ReadInfo(XmlReader xmlReader)
        {
            MessageInfo Info = null;
            while (xmlReader.ReadToFollowing(nameof(Info)) && xmlReader.Depth == 1)
            {
                if (Info != null)
                {
                    throw new XmlException($"Element '{nameof(Info)}' must not exist more than once.");
                }

                Info = new MessageInfo();

                // We are using "ReadElementContentAs..." which will consume the element and place the "cursor"
                // just before the next element. If this was already done there must not be another xmlReader.Read()

                var alreadyMovedToNextElement = false;

                while (!xmlReader.EOF)
                {
                    if (!alreadyMovedToNextElement)
                    {
                        if (!xmlReader.Read())
                            break;
                    }

                    alreadyMovedToNextElement = false;

                    if (xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name.Equals(nameof(Info)))
                    {
                        break;
                    }

                    if (xmlReader.NodeType != XmlNodeType.Element) continue;

                    switch (xmlReader.Name)
                    {
                        case nameof(Info.Id):
                            Info.Id = xmlReader.ReadElementContentAsLong();
                            alreadyMovedToNextElement = true;
                            break;
                        case nameof(Info.Category):
                            Info.Category = xmlReader.ReadElementContentAsString();
                            alreadyMovedToNextElement = true;
                            break;
                        case nameof(Info.Description):
                            Info.Description = xmlReader.ReadElementContentAsString();
                            alreadyMovedToNextElement = true;
                            break;
                        case nameof(Info.Comments):
                            Info.Comments = xmlReader.ReadElementContentAsString();
                            alreadyMovedToNextElement = true;
                            break;
                        case nameof(Info.Data): // may be CDATA or a pure string value
                            Info.Data = xmlReader.ReadElementContentAsString();
                            alreadyMovedToNextElement = true;
                            break;
                        default:
                            throw new XmlException($"Illegal element found inside parent element '{nameof(Info)}'.");
                    }
                }
            }

            if (Info == null)
            {
                throw new XmlException($"XML does not contain an element '{nameof(Info)}'.");
            }

            return Info;
        }

        #endregion
    }
}