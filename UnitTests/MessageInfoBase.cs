using System.Xml;
using MailMergeLib.MessageStore;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class MessageInfoBase
    {
        [Test]
        public void ReadInfo()
        {
            var info = new MessageInfo() {Id = 1, Category = "Some cat.", Comments = "Somme comments", Description = "No description", Data = "Some data hint"};

            var xml = "<MailMergeMessage>" +
                          "<Info>" +
                              $"<Id>{info.Id}</Id>\n" +
                              $"<Category>{info.Category}</Category>" +
                              $"<Description>{info.Description}</Description>\n\r" +
                              $"<Comments>{info.Comments}</Comments>" +
                              $"<Data><![CDATA[{info.Data}]]></Data>" +
                          "</Info>" +
                      "</MailMergeMessage>";

            Assert.AreEqual(info, MailMergeLib.MessageStore.MessageInfoBase.Read(xml));
        }

        [Test]
        public void ReadInfo_BadElementInsideInfo()
        {
            var info = new MessageInfo() { Id = 1, Category = "Some cat.", Comments = "Somme comments", Description = "No description", Data = "Some data hint" };

            var xml = "<MailMergeMessage>" +
                          "<Info>" +
                          $"<BadElement>{info.Id}</BadElement>\n" +
                          "</Info>" +
                      "</MailMergeMessage>";

            Assert.Throws<XmlException>(() => MailMergeLib.MessageStore.MessageInfoBase.Read(xml));
        }

        [Test]
        public void ReadInfo_ButInfoElementIsMissing()
        {
            var info = new MessageInfo() { Id = 1, Category = "Some cat.", Comments = "Somme comments", Description = "No description", Data = "Some data hint" };

            var xml = "<MailMergeMessage>" +
                          "<InfoMissing>" +
                          $"<Id>{info.Id}</Id>\n" +
                          "</InfoMissing>" +
                      "</MailMergeMessage>";

            Assert.Throws<XmlException>(() => MailMergeLib.MessageStore.MessageInfoBase.Read(xml));
        }

        [Test]
        public void ReadInfo_WithTwoInfoElements()
        {
            var info = new MessageInfo() { Id = 1, Category = "Some cat.", Comments = "Somme comments", Description = "No description", Data = "Some data hint" };

            var xml = "<MailMergeMessage>" +
                          "<Info>" +
                            $"<Id>{info.Id}</Id>\n" +
                          "</Info>" +
                          "<Info>" +
                            $"<Id>{info.Id}</Id>\n" +
                          "</Info>" + 
                      "</MailMergeMessage>";

            Assert.Throws<XmlException>(() => MailMergeLib.MessageStore.MessageInfoBase.Read(xml));
        }
    }
}
