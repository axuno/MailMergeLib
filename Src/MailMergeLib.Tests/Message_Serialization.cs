using System;
using System.IO;
using System.Linq;
using System.Text;
using MailMergeLib.Templates;
using NUnit.Framework;

namespace MailMergeLib.Tests;

[TestFixture]
public class Message_Serialization
{
    [Test]
    public void SerializationFromToString()
    {
        var mmm = MessageFactory.GetMessageWithAllPropertiesSet();
        var result = mmm.Serialize();
        var back = MailMergeMessage.Deserialize(result)!;

        Assert.Multiple(() =>
        {
            Assert.That(mmm.Equals(back), Is.True);
            Assert.That(back.Serialize(), Is.EqualTo(mmm.Serialize()));
        });
    }

    [Test]
    public void SerializationFromToFile()
    {
        var filename = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var mmm = MessageFactory.GetMessageWithAllPropertiesSet();
        mmm.Serialize(filename, Encoding.Unicode);
        var back = MailMergeMessage.Deserialize(filename, Encoding.Unicode)!;

        Assert.Multiple(() =>
        {
            Assert.That(mmm.Equals(back), Is.True);
            Assert.That(back.Serialize(), Is.EqualTo(mmm.Serialize()));
        });
    }

    [Test]
    public void SerializationFromToStream()
    {
        var mmm = MessageFactory.GetMessageWithAllPropertiesSet();
        var msOut = new MemoryStream();
        mmm.Serialize(msOut, Encoding.UTF8);
        msOut.Position = 0;

        var back = MailMergeMessage.Deserialize(msOut, Encoding.UTF8)!;
        msOut.Close();
        msOut.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(mmm.Equals(back), Is.True);
            Assert.That(back.Serialize(), Is.EqualTo(mmm.Serialize()));
        });
    }

    [Test]
    public void MessageClearExternalInlineAttachments()
    {
        var mmm = MessageFactory.GetMessageWithAllPropertiesSet();
        mmm.MailMergeAddresses.Clear();
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "test1@abc.com"));
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "test2@abc.com"));

        var mime = mmm.GetMimeMessage(new {Date = DateTime.Now, Success = true, Name = "Joe"});
        Assert.That(mime.BodyParts.FirstOrDefault(mbp => mbp.ContentId == "error-image.jpg") != null, Is.True);

        mmm.ClearExternalInlineAttachment();
        mime = mmm.GetMimeMessage(new { Date = DateTime.Now, Success = true, Name = "Joe" });
        Assert.That(mime.BodyParts.FirstOrDefault(mbp => mbp.ContentId == "error-image.jpg") == null, Is.True);
    }

    [Test]
    public void DeserializeMinimalisticXml()
    {
        // an empty deserialized message and new message must be equal
        var mmm = MailMergeMessage.Deserialize("<MailMergeMessage></MailMergeMessage>")!;
        Assert.That(new MailMergeMessage().Equals(mmm), Is.True);
    }

    [Test]
    public void SerializeNewMailMergeMessage()
    {
        Assert.DoesNotThrow(() => new MailMergeMessage().Serialize()); 
    }

    [Test]
    public void SerializeTemplates()
    {
        var templates = new Templates.Templates
        {
            new Template()
            {
                Name = "TestTemplate",
                Text = new Parts
                {
                    new Part(PartType.Plain, "key1", "some text"),
                    new Part(PartType.Plain, "key2", "other text")
                }
            }
        };
        var result = templates.Serialize();
        var back = new MailMergeMessage();
        back.Templates.AddRange(Templates.Templates.Deserialize(result)!);

        Assert.Multiple(() =>
        {
            Assert.That(templates.Equals(back.Templates), Is.True);
            Assert.That(back.Templates.Serialize(), Is.EqualTo(templates.Serialize()));
        });
    }
}
