using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using MailMergeLib;
using MailMergeLib.Templates;

namespace UnitTests
{
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
            var templates = new Templates
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
            back.Templates.AddRange(Templates.Deserialize(result));

            Assert.True(templates.Equals(back.Templates));
            Assert.AreEqual(templates.Serialize(), back.Templates.Serialize());
        }
    }
}
