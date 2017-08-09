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
            var fms = new FileMessageStore(new[] { TestFileFolders.FilesAbsPath }, new[] { "Msg*.xml" });
            Assert.AreEqual(fms, FileMessageStore.Deserialize(fms.Serialize()));
        }

        [Test]
        public void GetMessageInfosFromFiles()
        {
            var fms = new FileMessageStore(new[] { TestFileFolders.FilesAbsPath }, new[] {"Msg*.xml"});
            var messageInfos = fms.ScanForMessages().ToList();

            Assert.AreEqual(2, messageInfos.Count);

            foreach (var info in messageInfos)
            {
                // messageInfos come from fast xml scan in MessageInfoBase, Info of the Messsage comes from YAXLib deserialization
                Assert.AreEqual(info, info.LoadMessage(Encoding.UTF8).Info);
            }
        }

        [Test]
        public void NoMessageFilesFound()
        {
            var fms = new FileMessageStore(new[] { TestFileFolders.FilesAbsPath }, new[] { Guid.NewGuid().ToString("N") });
            var messageInfos = fms.ScanForMessages().ToList();
            Assert.AreEqual(0, messageInfos.Count);

            fms.SearchFolders = new[] { TestFileFolders.FilesAbsPath + Guid.NewGuid().ToString("N")};
            Assert.Throws<DirectoryNotFoundException>(() => messageInfos = fms.ScanForMessages().ToList());
        }
    }
}
