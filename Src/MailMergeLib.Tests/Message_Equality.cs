using System.Linq;
using System.Text;
using NUnit.Framework;

namespace MailMergeLib.Tests;

[TestFixture]
public class Message_Equality
{
    private readonly MailMergeAddress _addr1a = new(MailAddressType.To, "display name", "address@test.com") { DisplayNameCharacterEncoding = Encoding.UTF32 };
    private readonly MailMergeAddress _addr2a = new(MailAddressType.Bcc, "display name", "address@test.com") { DisplayNameCharacterEncoding = Encoding.UTF32 };

    private readonly MailMergeAddress _addr3 = new(MailAddressType.From, "display name 3", "address3@test.com") { DisplayNameCharacterEncoding = Encoding.UTF8 };

    private readonly MailMergeAddress _addr1b = new(MailAddressType.To, "display name", "address@test.com") { DisplayNameCharacterEncoding = Encoding.UTF32 };
    private readonly MailMergeAddress _addr2b = new(MailAddressType.Bcc, "display name", "address@test.com") { DisplayNameCharacterEncoding = Encoding.UTF32 };

    [Test]
    public void MailMergeAddressEquality()
    {
        Assert.That(_addr1a.Equals(_addr1b), Is.True);
        Assert.That(_addr1a.Equals(_addr3), Is.False);
    }

    [Test]
    public void MailMergeAddressCollectionEquality()
    {
        var addrColl1 = new MailMergeAddressCollection {_addr1a, _addr2a};
        var addrColl2 = new MailMergeAddressCollection {_addr2b, _addr1b};
        var addrColl1_Ref = addrColl1;

        Assert.Multiple(() =>
        {
            Assert.That(addrColl1.Equals(addrColl2), Is.True);
            Assert.That(addrColl1.Equals(addrColl1_Ref), Is.True);
        });
        Assert.That(addrColl1.Equals(_addr1a), Is.False);

        addrColl2.Add(_addr3);
        Assert.That(addrColl1.Equals(addrColl2), Is.False);
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
                Assert.That(addrColl.Get(addrType).FirstOrDefault(), Is.EqualTo(_addr1a));
                break;
            case MailAddressType.Bcc:
                Assert.That(addrColl.Get(addrType).FirstOrDefault(), Is.EqualTo(_addr2a));
                break;
            case MailAddressType.From:
                Assert.That(addrColl.Get(addrType).FirstOrDefault(), Is.EqualTo(_addr3));
                break;
            default:
                Assert.That(addrColl.Get(addrType).FirstOrDefault(), Is.EqualTo(null));
                break;
        }
    }

    [Test]
    public void MailMergeAddressCollectionToString()
    {
        var mmm = new MailMergeMessage();
        mmm.MailMergeAddresses.Add(_addr1a);
        mmm.MailMergeAddresses.Add(_addr2a);

        Assert.That(mmm.MailMergeAddresses.ToString(MailAddressType.To, null), Is.EqualTo($"\"{_addr1a.DisplayName}\" <{_addr1a.Address}>"));
    }

    [Test]
    public void MailMergeAddressCollectionHashCode()
    {
        var mmm = new MailMergeMessage();
        mmm.MailMergeAddresses.Add(_addr1a);
        mmm.MailMergeAddresses.Add(_addr2a);
        mmm.MailMergeAddresses.Add(_addr3);
        var addrColl = new MailMergeAddressCollection { _addr1a, _addr2a, _addr3 };

        Assert.That(addrColl.GetHashCode(), Is.EqualTo(mmm.MailMergeAddresses.GetHashCode()));
    }

    [Test]
    public void FileAttachments()
    {
        var fa1 = new FileAttachment("filename", "display name", "txt/html");
        var fa2 = new FileAttachment("filename", "display name", "txt/html");
        var fa3 = new FileAttachment("filename 3", "display name", "txt/html");

        Assert.That(fa1.Equals(fa2), Is.True);
        Assert.That(fa1.Equals(fa3), Is.False);
    }

    [Test]
    public void StringAttachments()
    {
        var sa1 = new StringAttachment("Content", "display name", "txt/html");
        var sa2 = new StringAttachment("Content", "display name", "txt/html");
        var sa3 = new StringAttachment("Content", "display name", "txt/plain");

        Assert.That(sa1.Equals(sa2), Is.True);
        Assert.That(sa1.Equals(sa3), Is.False);
    }

    [Test]
    public void MailMergeMessage()
    {
        var mmm1 = MessageFactory.GetMessageWithAllPropertiesSet()!;
        var mmm2 = MailMergeLib.MailMergeMessage.Deserialize(mmm1.Serialize())!;
        var mmm3 = MessageFactory.GetMessageWithAllPropertiesSet()!;

        Assert.That(mmm1.Equals(mmm2), Is.True);
        Assert.That(mmm1.Equals(mmm2), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(mmm1, Is.EqualTo(mmm2));
            Assert.That(mmm1, Is.EqualTo(mmm3));
        });

        mmm2.HtmlText += "?";
        Assert.That(mmm1.Equals(mmm2), Is.False);

        mmm3.MailMergeAddresses.RemoveAt(0);
        Assert.Multiple(() =>
        {
            Assert.That(mmm1.Equals(mmm3), Is.False);

            Assert.That(mmm1.Equals(mmm1), Is.True);
            Assert.That(mmm1.Equals(new object()), Is.False);
        });
    }

    [Test]
    public void MailMergeMessageDispose()
    {
        var mmm1 = MessageFactory.GetMessageWithAllPropertiesSet();
        Assert.DoesNotThrow(() => mmm1.Dispose());
    }
}
