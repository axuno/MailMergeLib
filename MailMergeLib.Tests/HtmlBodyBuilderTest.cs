using System;
using NUnit.Framework;

namespace MailMergeLib.Tests
{
    [TestFixture]
    public class HtmlBodyBuilderTest
    {
        [TestCase("Temp")]
        [TestCase("..\\..\\temp")]
        public void SetHtmlBuilderDocBaseUri_UriFormatException(string baseUri)
        {
            var mmm = new MailMergeMessage("subject", "plain text", "<html><head><base href=\"\" /></head><body></body></html>");
            var hbb = new HtmlBodyBuilder(mmm, (object)null);
            Assert.Throws<UriFormatException>(() => hbb.DocBaseUri = baseUri);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("/tmp", IncludePlatform="Linux")]
        [TestCase("C:\\Temp", ExcludePlatform="Linux")]
        [TestCase("\\\\some\\unc\\path", ExcludePlatform="Linux")]
        public void SetHtmlBuilderDocBaseUri_NoException(string baseUri)
        {
            var mmm = new MailMergeMessage("subject", "plain text", "<html><head><base href=\"\" /></head><body></body></html>");
            var hbb = new HtmlBodyBuilder(mmm, (object)null);
            Assert.DoesNotThrow(() => hbb.DocBaseUri = baseUri);
        }

        [Test]
        public void ScriptTagRemoved()
        {
            var mmm = new MailMergeMessage("subject_to_set", "plain text", "<html><head><script>var x='x';</script><script>var y='y';</script></head><body>some body</body></html>");
            var hbb = new HtmlBodyBuilder(mmm, (object)null);
            var html = hbb.GetBodyPart();
            Assert.IsTrue(html.ToString().Contains("some body"));
            Assert.IsTrue(!html.ToString().Contains("script"));
        }

        [Test]
        public void ExistingTitleTagSetWithSubject()
        {
            var subjectToSet = "subject_to_set";
            var mmm = new MailMergeMessage(subjectToSet, "plain text", "<html><head><title>abc</title></head><body></body></html>");
            var hbb = new HtmlBodyBuilder(mmm, (object)null);
            var html = hbb.GetBodyPart();
            Assert.IsTrue(html.ToString().Contains(subjectToSet));
        }

        [Test]
        public void NonExistingTitleTagSetWithSubject()
        {
            var subjectToSet = "subject_to_set";
            var mmm = new MailMergeMessage(subjectToSet, "plain text", "<html><head></head><body></body></html>");
            var hbb = new HtmlBodyBuilder(mmm, (object)null);
            var html = hbb.GetBodyPart();
            Assert.IsTrue(!html.ToString().Contains(subjectToSet));
        }
    }
}
