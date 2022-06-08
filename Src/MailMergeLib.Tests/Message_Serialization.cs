using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using MailMergeLib.Templates;
using MimeKit;
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
        var back = MailMergeMessage.Deserialize(result);

        Assert.True(mmm.Equals(back));
        Assert.AreEqual(mmm.Serialize(), back.Serialize());
    }

    [Test]
    public void SerializationFromToFile()
    {
        var filename = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var mmm = MessageFactory.GetMessageWithAllPropertiesSet();
        mmm.Serialize(filename, Encoding.Unicode);
        var back = MailMergeMessage.Deserialize(filename, Encoding.Unicode);

        Assert.True(mmm.Equals(back));
        Assert.AreEqual(mmm.Serialize(), back.Serialize());
    }

    [Test]
    public void SerializationFromToStream()
    {
        var mmm = MessageFactory.GetMessageWithAllPropertiesSet();
        var msOut = new MemoryStream();
        mmm.Serialize(msOut, Encoding.UTF8);
        msOut.Position = 0;

        var back = MailMergeMessage.Deserialize(msOut, Encoding.UTF8);
        msOut.Close();
        msOut.Dispose();

        Assert.True(mmm.Equals(back));
        Assert.AreEqual(mmm.Serialize(), back.Serialize());
    }

    [Test]
    public void MessageClearExternalInlineAttachments()
    {
        var mmm = MessageFactory.GetMessageWithAllPropertiesSet();
        mmm.MailMergeAddresses.Clear();
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "test1@abc.com"));
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "test2@abc.com"));

        var mime = mmm.GetMimeMessage(new {Date = DateTime.Now, Success = true, Name = "Joe"});
        Assert.IsTrue(mime.BodyParts.FirstOrDefault(mbp => mbp.ContentId == "error-image.jpg") != null);

        mmm.ClearExternalInlineAttachment();
        mime = mmm.GetMimeMessage(new { Date = DateTime.Now, Success = true, Name = "Joe" });
        Assert.IsTrue(mime.BodyParts.FirstOrDefault(mbp => mbp.ContentId == "error-image.jpg") == null);
    }

    [Test]
    public void DeserializeMinimalisticXml()
    {
        // an empty deserialized message and new message must be equal
        var mmm = MailMergeMessage.Deserialize("<MailMergeMessage></MailMergeMessage>");
        Assert.True(new MailMergeMessage().Equals(mmm));
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
        back.Templates.AddRange(Templates.Templates.Deserialize(result));

        Assert.True(templates.Equals(back.Templates));
        Assert.AreEqual(templates.Serialize(), back.Templates.Serialize());
    }

    [Test]
    public void StringAsCdataSerialilzer()
    {
        var cdata = new MailMergeLib.Serialization.StringAsCdataSerializer();
        Assert.Throws<NotImplementedException>(() => { cdata.SerializeToAttribute(string.Empty, new XAttribute("n", "v")); });
        Assert.Throws<NotImplementedException>(() => { cdata.SerializeToValue(string.Empty); });
        Assert.Throws<NotImplementedException>(() => { cdata.DeserializeFromAttribute(new XAttribute("n", "v")); });
        Assert.Throws<NotImplementedException>(() => { cdata.DeserializeFromValue(string.Empty); });
    }

    [Test]
    public void IPEndPointSerializer()
    {
        var eps = new MailMergeLib.Serialization.IPEndPointSerializer();
        Assert.Throws<NotImplementedException>(() => { eps.SerializeToAttribute(null, new XAttribute("n", "v")); });
        Assert.Throws<NotImplementedException>(() => { eps.SerializeToValue(null); });
        Assert.Throws<NotImplementedException>(() => { eps.DeserializeFromAttribute(new XAttribute("n", "v")); });
        Assert.Throws<NotImplementedException>(() => { eps.DeserializeFromValue(string.Empty); });
    }

    [Test]
    public void HeaderListSerializer()
    {
        var hl = new MailMergeLib.Serialization.HeaderListSerializer();
        Assert.Throws<NotImplementedException>(() => { hl.SerializeToAttribute(new HeaderList(), new XAttribute("n", "v")); });
        Assert.Throws<NotImplementedException>(() => { hl.SerializeToValue(new HeaderList()); });
        Assert.Throws<NotImplementedException>(() => { hl.DeserializeFromAttribute(new XAttribute("n", "v")); });
        Assert.Throws<NotImplementedException>(() => { hl.DeserializeFromValue(string.Empty); });
    }

    [Test]
    public void PartSerializer()
    {
        var part = new MailMergeLib.Serialization.PartSerializer();
        Assert.Throws<NotImplementedException>(() => { part.SerializeToAttribute(new Part(), new XAttribute("n", "v")); });
        Assert.Throws<NotImplementedException>(() => { part.SerializeToValue(new Part()); });
        Assert.Throws<NotImplementedException>(() => { part.DeserializeFromAttribute(new XAttribute("n", "v")); });
        Assert.Throws<NotImplementedException>(() => { part.DeserializeFromValue(string.Empty); });
    }

    [Test]
    public void MailMergeAddressesSerializer()
    {
        var mmAddr = new MailMergeLib.Serialization.MailMergeAddressesSerializer();
        Assert.Throws<NotImplementedException>(() => { mmAddr.SerializeToAttribute(new MailMergeAddressCollection(), new XAttribute("n", "v")); });
        Assert.Throws<NotImplementedException>(() => { mmAddr.SerializeToValue(new MailMergeAddressCollection()); });
        Assert.Throws<NotImplementedException>(() => { mmAddr.DeserializeFromAttribute(new XAttribute("n", "v")); });
        Assert.Throws<NotImplementedException>(() => { mmAddr.DeserializeFromValue(string.Empty); });
    }
}