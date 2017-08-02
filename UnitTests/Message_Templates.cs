using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MailMergeLib;
using MailMergeLib.Templates;

namespace UnitTests
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

            mmm.Templates[0].Text.RemoveAt(5);
            // This will remove the last entry with the key "Formal", which would leave an illegal mmm.Templates[0].DefaultKey
            Assert.Throws<TemplateException>(() => mmm.Templates[0].Text.RemoveAt(4));
        }
    }
}
