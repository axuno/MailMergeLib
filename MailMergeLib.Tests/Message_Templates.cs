using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MailMergeLib.Templates;
using NUnit.Framework;

namespace MailMergeLib.Tests
{
    [TestFixture]
    class Message_Templates
    {
        [Test]
        public void Template()
        {
            var mmm = MessageFactory.GetHtmlAndPlainMessage_WithTemplates(out Dictionary<string, string> variables);

            var msg = mmm.GetMimeMessage(variables);

            Assert.AreEqual(mmm.Subject.Replace("{FirstName}", variables["FirstName"]), msg.Subject);

            // DefaultKey is "Formal"
            Assert.IsTrue(msg.HtmlBody.Contains(mmm.Templates["Salutation"]["Formal"]
                .First(t => t.Type == PartType.Html)
                .Value.Replace("{FirstName}", variables["FirstName"])));
            Assert.IsTrue(msg.TextBody.Contains(mmm.Templates["Salutation"]["Formal"]
                .First(t => t.Type == PartType.Plain)
                .Value.Replace("{FirstName}", variables["FirstName"])));

            // Programmacically set the part to use
            mmm.Templates[0].Key = "Dear";
            msg = mmm.GetMimeMessage(variables);
            Assert.IsTrue(msg.HtmlBody.Contains(mmm.Templates["Salutation"]["Dear"]
                .First(t => t.Type == PartType.Html)
                .Value.Replace("{FirstName}", variables["FirstName"])));
            Assert.IsTrue(msg.TextBody.Contains(mmm.Templates["Salutation"]["Dear"]
                .First(t => t.Type == PartType.Plain)
                .Value.Replace("{FirstName}", variables["FirstName"])));

            // Neither DefaultKey nore Key of the template are set: gets the first part
            mmm.Templates[0].DefaultKey = null;
            mmm.Templates[0].Key = null;
            // Remove so that only max. 2 parts for 1 key are left
            mmm.Templates[0].Text.Remove(mmm.Templates[0].Text[5]);
            mmm.Templates[0].Text.Remove(mmm.Templates[0].Text[4]);
            mmm.Templates[0].Text.Remove(mmm.Templates[0].Text[3]);
            mmm.Templates[0].Text.Remove(mmm.Templates[0].Text[2]);

            msg = mmm.GetMimeMessage(variables);
            Assert.IsTrue(msg.HtmlBody.Contains(mmm.Templates["Salutation"]["Hi"]
                .First(t => t.Type == PartType.Html)
                .Value.Replace("{FirstName}", variables["FirstName"])));
            Assert.IsTrue(msg.TextBody.Contains(mmm.Templates["Salutation"]["Hi"]
                .First(t => t.Type == PartType.Plain)
                .Value.Replace("{FirstName}", variables["FirstName"])));
        }

        [Test]
        public void Part_in_Template_Changes()
        {
            var mmm = MessageFactory.GetHtmlAndPlainMessage_WithTemplates(out Dictionary<string, string> variables);

            Assert.Throws<TemplateException>(() => mmm.Templates["This-is-definitely-an-illegal-Key-not-existing-in-any-Part"] = new Template());
            
            // Assign a template
            Assert.DoesNotThrow(() => mmm.Templates[mmm.Templates[0].Name] = mmm.Templates[0]);

            // adding a template with the same key is not allowed
            Assert.Throws<TemplateException>(() => mmm.Templates.Add(mmm.Templates[0]));

            mmm.Templates[0].Text.RemoveAt(5);
            // This will remove the last entry with the key "Formal", which would leave an illegal mmm.Templates[0].DefaultKey
            Assert.Throws<TemplateException>(() => mmm.Templates[0].Text.RemoveAt(4));
        }

        [Test]
        public void FileSerialization()
        {
            var mmm = MessageFactory.GetHtmlAndPlainMessage_WithTemplates(out Dictionary<string, string> variables);
            var templates = mmm.Templates;
            var tempFilename = Path.GetTempFileName();
            templates.Serialize(tempFilename, Encoding.UTF8);
            Assert.True(templates.Equals(Templates.Templates.Deserialize(tempFilename, Encoding.UTF8)));
            File.Delete(tempFilename);
        }

        [Test]
        public void StreamSerialization()
        {
            var mmm = MessageFactory.GetHtmlAndPlainMessage_WithTemplates(out Dictionary<string, string> variables);
            var templates = mmm.Templates;
            var stream = new MemoryStream();
            templates.Serialize(stream, Encoding.UTF8);
            stream.Position = 0;
            var restoredTemplates = Templates.Templates.Deserialize(stream, Encoding.UTF8);
            Assert.True(templates.Equals(restoredTemplates));

            Assert.True(templates.Equals(templates));
            Assert.False(templates.Equals(new object()));
        }

        [Test]
        public void TemplateTest()
        {
            var mmm = MessageFactory.GetHtmlAndPlainMessage_WithTemplates(out Dictionary<string, string> variables);
            var t = mmm.Templates[0];
            Assert.DoesNotThrow(() =>
            {
                Assert.IsTrue(t.GetParts(null).Length > 0);
            });
            Assert.Throws<TemplateException>(() => t.Key = "This-is-definitely-an-illegal-Key-not-existing-in-any-Part");

            Assert.False(t.Equals(null));
            Assert.True(t.Equals(t));
            Assert.False(t.Equals(new object()));

            // Equality with null members
            t.Name = t.Key = t.DefaultKey = null;
            Assert.False(t.Equals(null));
            Assert.True(t.Equals(t));
            Assert.False(t.Equals(new object()));
        }
    }
}
