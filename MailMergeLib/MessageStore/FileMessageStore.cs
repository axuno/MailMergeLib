using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        {
        }

        /// <summary>
        /// Constructor of the <see cref="FileMessageStore"/> class.
        /// </summary>
        /// <param name="searchFolders"></param>
        /// <param name="searchPatterns"></param>
        public FileMessageStore(string[] searchFolders, string[] searchPatterns)
        {
            SearchFolders = searchFolders;
            SearchPatterns = searchPatterns;
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
                    MessageFile = fileInfo
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
            var serializer = SerializationFactory.GetStandardSerializer(typeof(FileMessageStore));
            return serializer.Serialize(this);
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
        /// Write a message store with a StreamWriter.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="isStream">If true, the writer will not be closed and disposed, so that the underlying stream can be used on return.</param>
        private void Serialize(TextWriter writer, bool isStream)
        {
            var serializer = SerializationFactory.GetStandardSerializer(typeof(FileMessageStore));
            serializer.Serialize(this, writer);
            writer.Flush();

            if (isStream) return;

#if NET40 || NET45
            writer.Close();
#endif
            writer.Dispose();
        }

        /// <summary>
        /// Reads a message store from an xml string.
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static FileMessageStore Deserialize(string xml)
        {
            var serializer = SerializationFactory.GetStandardSerializer(typeof(FileMessageStore));
            return (FileMessageStore)serializer.Deserialize(xml);
        }

        /// <summary>
        /// Reads a message store from an xml stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="encoding"></param>
        public static FileMessageStore Deserialize(Stream stream, System.Text.Encoding encoding)
        {
            return Deserialize(new StreamReader(stream, encoding), true);
        }

        /// <summary>
        /// Reads message store from an xml file.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="encoding"></param>
        public static FileMessageStore Deserialize(string filename, System.Text.Encoding encoding)
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
        /// Reads a message store xml with a StreamReader.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>Returns a message store instance</returns>
        /// <param name="isStream">If true, the writer will not be closed and disposed, so that the underlying stream can be used on return.</param>
        private static FileMessageStore Deserialize(StreamReader reader, bool isStream)
        {
            var serializer = SerializationFactory.GetStandardSerializer(typeof(FileMessageStore));
            reader.BaseStream.Position = 0;
            var str = reader.ReadToEnd();
            var s = (FileMessageStore)serializer.Deserialize(str);

            if (isStream) return s;
#if NET40 || NET45
            reader.Close();
#endif
            reader.Dispose();
            return s;
        }

        #endregion

        #region *** Equality ***

        protected bool Equals(FileMessageStore other)
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
            if (ReferenceEquals(null, obj)) return false;
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
                return ((SearchFolders != null ? SearchFolders.GetHashCode() : 0) * 397) ^ (SearchPatterns != null ? SearchPatterns.GetHashCode() : 0);
            }
        }

        #endregion
    }
}
