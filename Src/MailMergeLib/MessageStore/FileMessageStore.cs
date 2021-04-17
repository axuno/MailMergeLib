using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MailMergeLib.Serialization;
using YAXLib;

namespace MailMergeLib.MessageStore
{
    /// <summary>
    /// The class searches deserialized <see cref="MailMergeMessage"/> files in the file system,
    /// and reads their metadata (<see cref="IMessageInfo"/>).
    /// </summary>
    public class FileMessageStore : IMessageStore
    {
        /// <summary>
        /// Constructor of the <see cref="FileMessageStore"/> class.
        /// </summary>
        public FileMessageStore()
        {}

        /// <summary>
        /// The <see cref="Encoding"/> to apply when loading <see cref="MailMergeMessage"/>s.
        /// Used for serialization. It is the string representation of <see cref="MessageEncoding"/>.
        /// </summary>
        [YAXSerializableField]
        [YAXSerializeAs("MessageEncoding")]
        private string MessageEncodingName
        {
            get => MessageEncoding.WebName;
            set => MessageEncoding = Encoding.GetEncoding(value);
        }

        /// <summary>
        /// Gets or sets the <see cref="Encoding"/> to apply when loading <see cref="MailMergeMessage"/>s from the file system.
        /// </summary>
        /// <remarks><see cref="MessageEncoding"/> will also be used by <see cref="FileMessageInfo"/></remarks>
        [YAXDontSerialize]
        public Encoding MessageEncoding { get; set; } = Encoding.UTF8;

        /// <summary>
        /// Constructor of the <see cref="FileMessageStore"/> class.
        /// </summary>
        /// <param name="searchFolders"></param>
        /// <param name="searchPatterns"></param>
        /// <param name="encoding">The <see cref="Encoding"/> to apply when loading <see cref="MailMergeMessage"/>s.</param>
        public FileMessageStore(string[] searchFolders, string[] searchPatterns, Encoding encoding) : this()
        {
            SearchFolders = searchFolders;
            SearchPatterns = searchPatterns;
            MessageEncoding = encoding;
        }

        /// <summary>
        /// A list of absolute paths to file system folders where deserialized <see cref="MailMergeMessage"/> files are stored.
        /// </summary>
        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "Folder")]
        public string[] SearchFolders { get; set; } = {};

        /// <summary>
        /// The search pattern to use for getting the file names of deserialized <see cref="MailMergeMessage"/> files.
        /// Default search pattern is "*.*".
        /// </summary>
        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "Pattern")]
        public string[] SearchPatterns { get; set; } = {"*.*"};

        /// <summary>
        /// Scans all <see cref="SearchFolders"/> for deserialized <see cref="MailMergeMessage"/> files.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<MessageInfoBase> ScanForMessages()
        {
            foreach (var fileInfo in GetFiles(SearchFolders, SearchPatterns))
            {
                var info = MessageInfoBase.Read(fileInfo);
                var mi = new FileMessageInfo
                {
                    Id = info.Id,
                    Category = info.Category,
                    Description = info.Description,
                    Comments = info.Comments,
                    Data = info.Data,
                    MessageFile = fileInfo,
                    MessageEncoding = MessageEncoding
                };
                yield return mi;
            }
        }

        private static IEnumerable<FileInfo> GetFiles(IEnumerable<string> searchFolders, IEnumerable<string> searchPatterns, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            return from folder in searchFolders
                from pattern in searchPatterns
                from fileInfo in new DirectoryInfo(folder).GetFiles(pattern, searchOption)
                select fileInfo;
        }

        #region *** Serialization ***

        /// <summary>
        /// Get the message store as a serialized xml string.
        /// </summary>
        /// <returns></returns>
        public string Serialize()
        {
            return SerializationFactory.Serialize(this);
        }

        /// <summary>
        /// Write a message store to an xml stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="encoding"></param>
        public void Serialize(Stream stream, System.Text.Encoding encoding)
        {
            Serialize(new StreamWriter(stream, encoding), true);
        }

        /// <summary>
        /// Write message store to a file.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="encoding"></param>
        public void Serialize(string filename, Encoding encoding)
        {
            SerializationFactory.Serialize(this, filename, encoding);
        }

        /// <summary>
        /// Write a message store with a StreamWriter.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="isStream">If true, the writer will not be closed and disposed, so that the underlying stream can be used on return.</param>
        private void Serialize(TextWriter writer, bool isStream)
        {
            SerializationFactory.Serialize(this, writer, isStream);
        }

        /// <summary>
        /// Reads a message store from an xml string.
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static FileMessageStore Deserialize(string xml)
        {
            return SerializationFactory.Deserialize<FileMessageStore>(xml);
        }

        /// <summary>
        /// Reads a message store from an xml stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="encoding"></param>
        public static FileMessageStore Deserialize(Stream stream, System.Text.Encoding encoding)
        {
            return SerializationFactory.Deserialize<FileMessageStore>(stream, encoding);
        }

        /// <summary>
        /// Reads message store from an xml file.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="encoding"></param>
        public static FileMessageStore Deserialize(string filename, System.Text.Encoding encoding)
        {
            return SerializationFactory.Deserialize<FileMessageStore>(filename, encoding);
        }

        #endregion

        #region *** Equality ***

        private bool Equals(FileMessageStore other)
        {
            if (other == null) return false;
            return !SearchFolders.Except(other.SearchFolders).Union(other.SearchFolders.Except(SearchFolders)).Any() ||
                   !SearchPatterns.Except(other.SearchPatterns).Union(other.SearchPatterns.Except(SearchPatterns)).Any();
        }

        /// <summary>
        /// Compares this object with another.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FileMessageStore) obj);
        }

        /// <summary>
        /// Gets the hash code of this object.
        /// </summary>
        /// <returns>Returns the hash code of this object.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (SearchFolders != null
                           ? ((System.Collections.IStructuralEquatable) SearchFolders).GetHashCode(
                               EqualityComparer<string>.Default)
                           : 0) * 397
                       ^ (SearchPatterns != null
                           ? ((System.Collections.IStructuralEquatable) SearchPatterns).GetHashCode(
                               EqualityComparer<string>.Default)
                           : 0);
            }
        }

        #endregion
    }
}
