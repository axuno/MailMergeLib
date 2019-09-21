using System;
using System.IO;
using NUnit.Framework;

namespace MailMergeLib.Tests
{
    [TestFixture]
    public class Message_Config
    {
        private MessageConfig _msgConfig = new MessageConfig();

        [TestCase(" \t", "")]
        [TestCase(" ", "")]
        [TestCase("", "")]
        [TestCase(null, "")]
        [TestCase("noFullPath", "noFullPath")]
        [TestCase("C:\\some\\path\\to\\folder", "C:\\some\\path\\to\\folder")]
        public void SetFileBaseDirectory(string path, string expected)
        {
            _msgConfig.FileBaseDirectory = path;
            Assert.AreEqual(expected, _msgConfig.FileBaseDirectory);
        }

        [TestCase(" \t", false)]
        [TestCase(" ", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        [TestCase("C:\\some\\path\\to\\folder", false)]
        [TestCase("\\\\some\\unc\\path", false)]
        [TestCase("noFullPath", true)]
        [TestCase("..\\..\\relativePath", true)]
        public void FileBaseDirectory_must_be_full_path_when_processing_the_message(string path, bool shouldThrow)
        {
            var mmm = new MailMergeMessage("subject", "plain text", "<html><body></body></html>");
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "to@example.org"));
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "from@example.org"));
            mmm.Config.FileBaseDirectory = path;

            if (shouldThrow)
            {
                try
                {
                    mmm.GetMimeMessage(null);
                }
                catch (Exception e)
                {
                    Assert.IsTrue(e is MailMergeMessage.MailMergeMessageException);
                    Assert.IsTrue(e.InnerException != null);
                }
            }
            else
            {
                Assert.DoesNotThrow(() => mmm.GetMimeMessage(null));
            }
        }

        [TestCase(" \t", "file:///")]
        [TestCase(" ", "file:///")]
        [TestCase("", "file:///")]
        [TestCase("C:\\some\\path\\to\\folder", "file:///C:/some/path/to/folder")]
        [TestCase("\\\\some\\unc\\path", "file://some/unc/path")]
        [TestCase("noFullPath", null)]
        [TestCase("..\\..\\relativePath", null)]
        public void HtmlBodyBuilderDocBaseUri_vs_MessageConfig_FileBaseDirectory(string path, string expected)
        {
            var mmm = new MailMergeMessage("subject", "plain text", "<html><body></body></html>");
            mmm.Config.FileBaseDirectory = path;

            HtmlBodyBuilder hbb;
            if (expected == null)
            {
                Assert.Throws<UriFormatException>(() => { hbb = new HtmlBodyBuilder(mmm, (object) null); });
            }
            else
            {
                hbb = new HtmlBodyBuilder(mmm, (object) null);
                Assert.AreEqual(expected, hbb.DocBaseUri);
            }
        }

        [Test]
        public void MessageConfig_FileBaseDirectory_cannot_be_changed_by_Html_Base_Tag()
        {
            var mmm = new MailMergeMessage("subject", "plain text",
                "<html><head><base href=\"\" /></head><body></body></html>");
            mmm.Config.FileBaseDirectory = Path.GetTempPath();

            var hbb = new HtmlBodyBuilder(mmm, (object) null);
            Assert.AreEqual(new Uri(mmm.Config.FileBaseDirectory), hbb.DocBaseUri);
        }

        [Test]
        public void Empty_MessageConfig_FileBaseDirectory_is_changed_by_Html_Base_Tag()
        {
            var baseTagHref = "file:///C:/Temp/";
            var mmm = new MailMergeMessage("subject", "plain text",
                $"<html><head><base href=\"{baseTagHref}\" /></head><body></body></html>");
            mmm.Config.FileBaseDirectory = string.Empty;

            var hbb = new HtmlBodyBuilder(mmm, (object) null);
            hbb.GetBodyPart();
            Assert.AreEqual(baseTagHref, hbb.DocBaseUri);
        }

        [Test]
        public void HashCode()
        {
            var mc1 = new MessageConfig();
            var mc2 = new MessageConfig();

            Assert.AreEqual(mc1.GetHashCode(), mc2.GetHashCode());
            Assert.AreEqual(mc1.GetHashCode(), mc1.GetHashCode());
            Assert.AreEqual(mc2.GetHashCode(), mc2.GetHashCode());
        }
    }
}
