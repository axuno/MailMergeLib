using System.Linq;
using System.Text;
using MailMergeLib;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class Message_Equality
    {
        private MailMergeAddress _addr1a = new MailMergeAddress(MailAddressType.To, "display name", "address@test.com") { DisplayNameCharacterEncoding = Encoding.UTF32 };
        private MailMergeAddress _addr2a = new MailMergeAddress(MailAddressType.Bcc, "display name", "address@test.com") { DisplayNameCharacterEncoding = Encoding.UTF32 };

        private MailMergeAddress _addr3 = new MailMergeAddress(MailAddressType.From, "display name 3", "address3@test.com") { DisplayNameCharacterEncoding = Encoding.UTF8 };

        private MailMergeAddress _addr1b = new MailMergeAddress(MailAddressType.To, "display name", "address@test.com") { DisplayNameCharacterEncoding = Encoding.UTF32 };
        private MailMergeAddress _addr2b = new MailMergeAddress(MailAddressType.Bcc, "display name", "address@test.com") { DisplayNameCharacterEncoding = Encoding.UTF32 };

        [Test]
        public void MailMergeAddressEquality()
        {
            Assert.True(_addr1a.Equals(_addr1b));
            Assert.False(_addr1a.Equals(_addr3));
        }

        [Test]
        public void MailMergeAddressCollectionEquality()
        {
            var addrColl1 = new MailMergeAddressCollection {_addr1a, _addr2a};
            var addrColl2 = new MailMergeAddressCollection {_addr2b, _addr1b};
            var addrColl1_Ref = addrColl1;

            Assert.True(addrColl1.Equals(addrColl2));
            Assert.True(addrColl1.Equals(addrColl1_Ref));
            Assert.False(addrColl1.Equals(_addr1a));

            addrColl2.Add(_addr3);
            Assert.False(addrColl1.Equals(addrColl2));
            Assert.False(addrColl1.Equals(null));
        }

        [TestCase(MailAddressType.To)]
        [TestCase(MailAddressType.Bcc)]
        [TestCase(MailAddressType.From)]
        [TestCase(MailAddressType.Sender)]
        [TestCase(MailAddressType.CC)]
        [TestCase(MailAddressType.ConfirmReadingTo)]
        [TestCase(MailAddressType.ReplyTo)]
        [TestCase(MailAddressType.ReturnReceiptTo)]
        [TestCase(MailAddressType.TestAddress)]
        public void MailMergeAddressCollectionFind(MailAddressType addrType)
        {
            var addrColl = new MailMergeAddressCollection { _addr1a, _addr2a, _addr3 };

            switch (addrType)
            {
                case MailAddressType.To:
                    Assert.AreEqual(_addr1a, addrColl.Get(addrType).FirstOrDefault());
                    break;
                case MailAddressType.Bcc:
                    Assert.AreEqual(_addr2a, addrColl.Get(addrType).FirstOrDefault());
                    break;
                case MailAddressType.From:
                    Assert.AreEqual(_addr3, addrColl.Get(addrType).FirstOrDefault());
                    break;
                default:
                    Assert.AreEqual(null, addrColl.Get(addrType).FirstOrDefault());
                    break;
            }
        }

        [Test]
        public void MailMergeAddressCollectionToString()
        {
            var mmm = new MailMergeMessage();
            mmm.MailMergeAddresses.Add(_addr1a);
            mmm.MailMergeAddresses.Add(_addr2a);
            
            Assert.AreEqual($"\"{_addr1a.DisplayName}\" <{_addr1a.Address}>", mmm.MailMergeAddresses.ToString(MailAddressType.To, null));
        }

        [Test]
        public void MailMergeAddressCollectionHashCode()
        {
            var mmm = new MailMergeMessage();
            mmm.MailMergeAddresses.Add(_addr1a);
            mmm.MailMergeAddresses.Add(_addr2a);
            mmm.MailMergeAddresses.Add(_addr3);
            var addrColl = new MailMergeAddressCollection { _addr1a, _addr2a, _addr3 };

            Assert.AreEqual(mmm.MailMergeAddresses.GetHashCode(), addrColl.GetHashCode());
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

            Assert.IsFalse(mmm1.Equals(default(object)));
            Assert.IsTrue(mmm1.Equals(mmm1));
            Assert.IsFalse(mmm1.Equals(new object()));
        }

        [Test]
        public void MailMergeMessageDispose()
        {
            var mmm1 = MessageFactory.GetMessageWithAllPropertiesSet();
            Assert.DoesNotThrow(() => mmm1.Dispose());
        }
    }
}
