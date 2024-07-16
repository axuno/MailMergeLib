using System;
using System.IO;
using System.Linq;
using System.Text;
using MailMergeLib.MessageStore;
using NUnit.Framework;

namespace MailMergeLib.Tests;

[TestFixture]
public class FileMessageStore_Serialization
{
    [Test]
    public void SerializeDeserialize()
    {
        var fms = new FileMessageStore(new[] { TestFileFolders.FilesAbsPath }, new[] { "Msg*.xml" }, Encoding.UTF8);
        Assert.That(FileMessageStore.Deserialize(fms.Serialize()), Is.EqualTo(fms));
    }

    [Test]
    public void GetMessageInfosFromFiles()
    {
        var fms = new FileMessageStore(new[] { TestFileFolders.FilesAbsPath }, new[] { "Msg*.xml" }, Encoding.UTF8);
        var messageInfos = fms.ScanForMessages().ToList();

        Assert.That(messageInfos, Has.Count.EqualTo(2));

        foreach (var info in messageInfos)
        {
            // messageInfos come from fast xml scan in MessageInfoBase, Info of the Messsage comes from YAXLib deserialization
            Assert.That(info.Data, Is.EqualTo(info.LoadMessage()!.Info.Data));
        }
    }

    [Test]
    public void NoMessageFilesFound()
    {
        var fms = new FileMessageStore(new[] { TestFileFolders.FilesAbsPath }, new[] { Guid.NewGuid().ToString("N")}, Encoding.UTF8);
        var messageInfos = fms.ScanForMessages().ToList();
        Assert.That(messageInfos.Count, Is.EqualTo(0));

        fms.SearchFolders = new[] { TestFileFolders.FilesAbsPath + Guid.NewGuid().ToString("N")};
        Assert.Throws<DirectoryNotFoundException>(() => messageInfos = fms.ScanForMessages().ToList());
    }

    [Test]
    public void FileSerialization()
    {
        var fms = new FileMessageStore(new[] { TestFileFolders.FilesAbsPath }, new[] { Guid.NewGuid().ToString("N") }, Encoding.UTF8);
        var tempFilename = Path.GetTempFileName();
        fms.Serialize(tempFilename, Encoding.UTF8);
        Assert.That(fms.Equals(FileMessageStore.Deserialize(tempFilename, Encoding.UTF8)), Is.True);
        File.Delete(tempFilename);
    }

    [Test]
    public void StreamSerialization()
    {
        var fms = new FileMessageStore(new[] { TestFileFolders.FilesAbsPath }, new[] { Guid.NewGuid().ToString("N") }, Encoding.UTF8);
        var stream = new MemoryStream();
        fms.Serialize(stream, Encoding.UTF8);
        stream.Position = 0;
        var restoredFms = FileMessageStore.Deserialize(stream, Encoding.UTF8)!;
        Assert.Multiple(() =>
        {
            Assert.That(fms.Equals(restoredFms), Is.True);

            Assert.That(restoredFms.GetHashCode(), Is.EqualTo(fms.GetHashCode()));
        });
        Assert.That(fms.Equals(new object()), Is.False);
    }
}
