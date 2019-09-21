using System;
using System.IO;
using System.Linq;
using System.Text;
using MailMergeLib.MessageStore;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class FileMessageStore_Serialization
    {
        [Test]
        public void SerializeDeserialize()
        {
            var fms = new FileMessageStore(new[] { TestFileFolders.FilesAbsPath }, new[] { "Msg*.xml" }, Encoding.UTF8);
            Assert.AreEqual(fms, FileMessageStore.Deserialize(fms.Serialize()));
        }

        [Test]
        public void GetMessageInfosFromFiles()
        {
            var fms = new FileMessageStore(new[] { TestFileFolders.FilesAbsPath }, new[] {"Msg*.xml"}, Encoding.UTF8);
            var messageInfos = fms.ScanForMessages().ToList();

            Assert.AreEqual(2, messageInfos.Count);

            foreach (var info in messageInfos)
            {
                // messageInfos come from fast xml scan in MessageInfoBase, Info of the Messsage comes from YAXLib deserialization
                Assert.AreEqual(info, info.LoadMessage().Info);
            }
        }

        [Test]
        public void NoMessageFilesFound()
        {
            var fms = new FileMessageStore(new[] { TestFileFolders.FilesAbsPath }, new[] { Guid.NewGuid().ToString("N")}, Encoding.UTF8);
            var messageInfos = fms.ScanForMessages().ToList();
            Assert.AreEqual(0, messageInfos.Count);

            fms.SearchFolders = new[] { TestFileFolders.FilesAbsPath + Guid.NewGuid().ToString("N")};
            Assert.Throws<DirectoryNotFoundException>(() => messageInfos = fms.ScanForMessages().ToList());
        }

        [Test]
        public void FileSerialization()
        {
            var fms = new FileMessageStore(new[] { TestFileFolders.FilesAbsPath }, new[] { Guid.NewGuid().ToString("N") }, Encoding.UTF8);
            var tempFilename = Path.GetTempFileName();
            fms.Serialize(tempFilename, Encoding.UTF8);
            Assert.True(fms.Equals(FileMessageStore.Deserialize(tempFilename, Encoding.UTF8)));
            File.Delete(tempFilename);
        }

        [Test]
        public void StreamSerialization()
        {
            var fms = new FileMessageStore(new[] { TestFileFolders.FilesAbsPath }, new[] { Guid.NewGuid().ToString("N") }, Encoding.UTF8);
            var stream = new MemoryStream();
            fms.Serialize(stream, Encoding.UTF8);
            stream.Position = 0;
            var restoredFms = FileMessageStore.Deserialize(stream, Encoding.UTF8);
            Assert.True(fms.Equals(restoredFms));

            Assert.AreEqual(fms.GetHashCode(), restoredFms.GetHashCode());
            Assert.False(fms.Equals(null));
            Assert.True(fms.Equals(fms));
            Assert.False(fms.Equals(new object()));
        }
    }
}
