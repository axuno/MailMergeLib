using System.IO;
using System.Text;
using NUnit.Framework;

namespace MailMergeLib.Tests;

[TestFixture]
public class FileMessageInfo
{
    [Test]
    public void CompareFileMessageInfo()
    {
        var info = new MailMergeLib.MessageStore.FileMessageInfo { Id = 1, Category = "Some cat.", Comments = "Somme comments", Description = "No description", Data = "Some data hint", MessageEncoding = Encoding.UTF8, MessageFile = new FileInfo("dummy.xml")};
        var otherInfo = new MailMergeLib.MessageStore.FileMessageInfo { Id = 1, Category = "Some cat.", Comments = "Somme comments", Description = "No description", Data = "Some data hint", MessageEncoding = Encoding.UTF8, MessageFile = new FileInfo("dummy.xml") };

        Assert.Multiple(() =>
        {
            Assert.That(otherInfo, Is.EqualTo(info));
            Assert.That(info.Equals(null), Is.False);
            Assert.That(info.Equals(info), Is.True);
            Assert.That(info.Equals(new object()), Is.False);
        });

        Assert.That(otherInfo.GetHashCode(), Is.EqualTo(info.GetHashCode()));
    }

    [Test]
    public void CompareNullFileMessageInfo()
    {
        var info = new MailMergeLib.MessageStore.FileMessageInfo { Id = 1, Category = null, Comments = null, Description = "No description", Data = "Some data hint", MessageEncoding = null!, MessageFile = null! };
        var otherInfo = new MailMergeLib.MessageStore.FileMessageInfo { Id = 1, Category = null, Comments = null, Description = "No description", Data = "Some data hint", MessageEncoding = null!, MessageFile = null! };

        Assert.Multiple(() =>
        {
            Assert.That(otherInfo, Is.EqualTo(info));
            Assert.That(info.Equals(null), Is.False);
            Assert.That(info.Equals(info), Is.True);
            Assert.That(info.Equals(new object()), Is.False);
        });
        Assert.That(otherInfo.GetHashCode(), Is.EqualTo(info.GetHashCode()));
    }
}
