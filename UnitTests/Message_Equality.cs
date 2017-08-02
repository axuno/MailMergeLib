using System.Text;
using MailMergeLib;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class Message_Equality
    {
        [Test]
        public void MailMergeAddresses()
        {
            var addr1a = new MailMergeAddress(MailAddressType.To, "diplay name", "address@test.com") {DisplayNameCharacterEncoding = Encoding.UTF32};
            var addr2a = new MailMergeAddress(MailAddressType.To, "diplay name", "address@test.com") { DisplayNameCharacterEncoding = Encoding.UTF32 };

            var addr3 = new MailMergeAddress(MailAddressType.To, "diplay name 3", "address3@test.com") { DisplayNameCharacterEncoding = Encoding.UTF8 };

            var addr1b = new MailMergeAddress(MailAddressType.To, "diplay name", "address@test.com") { DisplayNameCharacterEncoding = Encoding.UTF32 };
            var addr2b = new MailMergeAddress(MailAddressType.To, "diplay name", "address@test.com") { DisplayNameCharacterEncoding = Encoding.UTF32 };

            Assert.True(addr1a.Equals(addr2a));
            Assert.False(addr1a.Equals(addr3));

            var addrColl1 = new MailMergeAddressCollection {addr1a, addr2a};
            var addrColl2 = new MailMergeAddressCollection {addr2b, addr1b};

            Assert.True(addrColl1.Equals(addrColl2));

            addrColl2.Add(addr3);
            Assert.False(addrColl1.Equals(addrColl2));
        }

        [Test]
        public void FileAttachments()
        {
            var fa1 = new FileAttachment("filename", "display name", "txt/html");
            var fa2 = new FileAttachment("filename", "display name", "txt/html");
            var fa3 = new FileAttachment("filename 3", "display name", "txt/html");

            Assert.True(fa1.Equals(fa2));
            Assert.False(fa1.Equals(fa3));
        }

        [Test]
        public void StringAttachments()
        {
            var sa1 = new StringAttachment("Content", "display name", "txt/html");
            var sa2 = new StringAttachment("Content", "display name", "txt/html");
            var sa3 = new StringAttachment("Content", "display name", "txt/plain");

            Assert.True(sa1.Equals(sa2));
            Assert.False(sa1.Equals(sa3));
        }

        [Test]
        public void MailMergeMessage()
        {
            var mmm1 = MessageFactory.GetMessageWithAllPropertiesSet();
            var mmm2 = MailMergeLib.MailMergeMessage.Deserialize(mmm1.Serialize());
            var mmm3 = MessageFactory.GetMessageWithAllPropertiesSet();

            Assert.IsTrue(mmm1.Equals(mmm2));
            Assert.IsTrue(mmm1.Equals(mmm3));

            mmm2.HtmlText += "?";
            Assert.IsFalse(mmm1.Equals(mmm2));

            mmm3.MailMergeAddresses.RemoveAt(0);
            Assert.IsFalse(mmm1.Equals(mmm3));
        }
    }
}
