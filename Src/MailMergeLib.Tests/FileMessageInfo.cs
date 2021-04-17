using System.IO;
using System.Text;
using NUnit.Framework;

namespace MailMergeLib.Tests
{
    [TestFixture]
    public class FileMessageInfo
    {
        [Test]
        public void CompareFileMessageInfo()
        {
            var info = new MailMergeLib.MessageStore.FileMessageInfo { Id = 1, Category = "Some cat.", Comments = "Somme comments", Description = "No description", Data = "Some data hint", MessageEncoding = Encoding.UTF8, MessageFile = new FileInfo("dummy.xml")};
            var otherInfo = new MailMergeLib.MessageStore.FileMessageInfo { Id = 1, Category = "Some cat.", Comments = "Somme comments", Description = "No description", Data = "Some data hint", MessageEncoding = Encoding.UTF8, MessageFile = new FileInfo("dummy.xml") };

            Assert.AreEqual(info, otherInfo);
            Assert.IsFalse(info.Equals(null));
            Assert.IsTrue(info.Equals(info));
            Assert.IsFalse(info.Equals(new object()));
            Assert.AreEqual(info.GetHashCode(), otherInfo.GetHashCode());
        }

        [Test]
        public void CompareNullFileMessageInfo()
        {
            var info = new MailMergeLib.MessageStore.FileMessageInfo { Id = 1, Category = null, Comments = null, Description = "No description", Data = "Some data hint", MessageEncoding = null, MessageFile = null };
            var otherInfo = new MailMergeLib.MessageStore.FileMessageInfo { Id = 1, Category = null, Comments = null, Description = "No description", Data = "Some data hint", MessageEncoding = null, MessageFile = null };

            Assert.AreEqual(info, otherInfo);
            Assert.IsFalse(info.Equals(null));
            Assert.IsTrue(info.Equals(info));
            Assert.IsFalse(info.Equals(new object()));
            Assert.AreEqual(info.GetHashCode(), otherInfo.GetHashCode());
        }
    }
}
