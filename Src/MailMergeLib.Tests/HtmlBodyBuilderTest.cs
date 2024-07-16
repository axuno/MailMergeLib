using System;
using MailMergeLib.Tests.NUnit;
using NUnit.Framework;

namespace MailMergeLib.Tests;

[TestFixture]
public class HtmlBodyBuilderTest
{
    [TestCase("Temp")]
    [TestCase("..\\..\\temp")]
    public void SetHtmlBuilderDocBaseUri_UriFormatException(string baseUri)
    {
        var mmm = new MailMergeMessage("subject", "plain text",
            "<html><head><base href=\"\" /></head><body></body></html>");
        var hbb = new HtmlBodyBuilder(mmm, null);
        Assert.Throws<UriFormatException>(() => hbb.DocBaseUri = baseUri);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("/tmp", IncludePlatform = nameof(OpSys.Linux) + "," + nameof(OpSys.MacOsX))]
    [TestCase("C:\\Temp", ExcludePlatform = nameof(OpSys.Linux) + "," + nameof(OpSys.MacOsX))]
    [TestCase("\\\\some\\unc\\path", ExcludePlatform = nameof(OpSys.Linux) + "," + nameof(OpSys.MacOsX))]
    public void SetHtmlBuilderDocBaseUri_NoException(string? baseUri)
    {
        var mmm = new MailMergeMessage("subject", "plain text",
            "<html><head><base href=\"\" /></head><body></body></html>");
        var hbb = new HtmlBodyBuilder(mmm, null);
        Assert.DoesNotThrow(() => hbb.DocBaseUri = baseUri!);
    }

    [Test]
    public void ScriptTagRemoved()
    {
        var mmm = new MailMergeMessage("subject_to_set", "plain text",
            "<html><head><script>var x='x';</script><script>var y='y';</script></head><body>some body</body></html>");
        var hbb = new HtmlBodyBuilder(mmm, null);
        var html = hbb.GetBodyPart();
        Assert.Multiple(() =>
        {
            Assert.That(html.ToString().Contains("some body"), Is.True);
            Assert.That(!html.ToString().Contains("script"), Is.True);
        });
    }

    [Test]
    public void ExistingTitleTagSetWithSubject()
    {
        var subjectToSet = "subject_to_set";
        var mmm = new MailMergeMessage(subjectToSet, "plain text",
            "<html><head><title>abc</title></head><body></body></html>");
        var hbb = new HtmlBodyBuilder(mmm, null);
        var html = hbb.GetBodyPart();
        Assert.That(html.ToString().Contains(subjectToSet), Is.True);
    }

    [Test]
    public void NonExistingTitleTagSetWithSubject()
    {
        var subjectToSet = "subject_to_set";
        var mmm = new MailMergeMessage(subjectToSet, "plain text", "<html><head></head><body></body></html>");
        var hbb = new HtmlBodyBuilder(mmm, null);
        var html = hbb.GetBodyPart();
        Assert.That(!html.ToString().Contains(subjectToSet), Is.True);
    }

    [Test]
    public void EmbeddedDataImage_ShouldNotBeTouched()
    {
        var image = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAAKAQMAAABPHKYJAAAABGdBTUEAALGPC/xhBQAAAAZQTFRFBPl7AAAAx+zuaAAAAAd0SU1FB9MEFwMzHmwS680AAAALSURBVBjTY2DABAAAFAABQpvU+wAAAABJRU5ErkJggg==";
        var imageTag = $"<img width=\"10\" height=\"10\" alt=\"1Pixel\" src=\"{image}\">";
        var mmm = new MailMergeMessage("", "plain text",
            $"<html><body>{imageTag}</body></html>");
        var hbb = new HtmlBodyBuilder(mmm, null);
        var html = hbb.GetBodyPart();
        Assert.That(html.ToString(), Does.Contain(imageTag));
    }

    [Test]
    public void LargeEmbeddedDataImage_ShouldNotThrow()
    {
        // Exceeding Uri size of 0xFFF0 would throw an UriFormatException,
        // if the embedded image was processed with new Uri(...)
            
        // Note: No need to be valid Base64 here:
        var image = "data:image/png;base64," + new string('a', 0xFFF0 + 1);
        var imageTag = $"<img width=\"10\" height=\"10\" alt=\"1Pixel\" src=\"{image}\">";
        var mmm = new MailMergeMessage("", "plain text",
            $"<html><body>{imageTag}</body></html>");
        var hbb = new HtmlBodyBuilder(mmm, null);
        Assert.That(code: () => hbb.GetBodyPart(), Throws.Nothing);
    }
}
