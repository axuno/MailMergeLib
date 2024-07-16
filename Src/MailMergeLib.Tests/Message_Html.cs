using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using MimeKit;
using NUnit.Framework;

namespace MailMergeLib.Tests;

[TestFixture]
public class Message_Html
{
    private class DummyHtmlConverter : IHtmlConverter
    {
        public const string ConstantText = "Plain text for test";
        public string ToPlainText(string html)
        {
            return ConstantText;
        }
    }

    [Test]
    public void MimeMessageSize()
    {
        using (var mmm = new MailMergeMessage("subject", "plain text", "<html><head></head><body>some body</body></html>"))
        {
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "to@example.org"));
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "from@example.org"));
            var mimeMessage = mmm.GetMimeMessage(null);

            var size = MailMergeLib.Tools.CalcMessageSize(mimeMessage);
            Assert.That(size > 0, Is.True);
        }

        Assert.That(MailMergeLib.Tools.CalcMessageSize(null) == 0, Is.True);
    }

    [Test]
    public void EmptyContent()
    {
        using var mmm = new MailMergeMessage();
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "to@example.org"));
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "from@example.org"));

        try
        {
            mmm.GetMimeMessage(null);
        }
        catch (Exception e)
        {
            Assert.Multiple(() =>
            {
                Assert.That(e is MailMergeMessage.MailMergeMessageException, Is.True);
                Assert.That(e.InnerException is MailMergeMessage.EmptyContentException, Is.True);
            });
        }
    }

    [Test]
    public void CreateTextMessageWithFileAttachments()
    {
        using var basicMmm = MessageFactory.GetHtmlMailWithInlineAndOtherAttachments();
        using var mmm = new MailMergeMessage("The subject", "plain text", basicMmm.FileAttachments);
        Assert.Multiple(() =>
        {
            Assert.That(mmm.Subject, Is.EqualTo("The subject"));
            Assert.That(mmm.PlainText, Is.EqualTo("plain text"));
            Assert.That(mmm.FileAttachments, Has.Count.EqualTo(basicMmm.FileAttachments.Count));
        });
    }

    [Test]
    public void CreateTextAndHtmlMessageWithFileAttachments()
    {
        using var basicMmm = MessageFactory.GetHtmlMailWithInlineAndOtherAttachments();
        using var mmm = new MailMergeMessage("The subject", "plain text", "<html>html</html>", basicMmm.FileAttachments);
        Assert.Multiple(() =>
        {
            Assert.That(mmm.Subject, Is.EqualTo("The subject"));
            Assert.That(mmm.PlainText, Is.EqualTo("plain text"));
            Assert.That(mmm.HtmlText, Is.EqualTo("<html>html</html>"));
            Assert.That(mmm.FileAttachments, Has.Count.EqualTo(basicMmm.FileAttachments.Count));
        });
    }

    [Test]
    public void HtmlMailMergeWithInlineAndAtt()
    {
        var dataItem = new
        {
            Name = "John",
            MailboxAddr = "john@example.com",
            Success = true,
            Date = DateTime.Now,
            SenderAddr = "test@specimen.com"
        };

        using var mmm = MessageFactory.GetHtmlMailWithInlineAndOtherAttachments();
        var msg = mmm.GetMimeMessage(dataItem);
        var msgFilename = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "test-msg.eml"));
        msg.WriteTo(msgFilename);
        Console.WriteLine($"Test mime message saved as {msgFilename}");

        Assert.Multiple(() =>
        {
            Assert.That(((MailboxAddress) msg.From.First()).Address == dataItem.SenderAddr, Is.True);
            Assert.That(((MailboxAddress) msg.To.First()).Address == dataItem.MailboxAddr, Is.True);
            Assert.That(((MailboxAddress) msg.To.First()).Name == dataItem.Name, Is.True);
            Assert.That(msg.Headers[HeaderId.Organization] == mmm.Config.Organization, Is.True);
            Assert.That(msg.Priority == mmm.Config.Priority, Is.True);
            Assert.That(msg.Attachments.FirstOrDefault(a => ((MimePart) a).FileName == "Log file from {Date:yyyy-MM-dd}.log".Replace("{Date:yyyy-MM-dd}", dataItem.Date.ToString("yyyy-MM-dd"))) != null, Is.True);
            Assert.That(msg.Subject == mmm.Subject.Replace("{Date:yyyy-MM-dd}", dataItem.Date.ToString("yyyy-MM-dd")), Is.True);
            Assert.That(msg.HtmlBody.Contains(dataItem.Success ? "succeeded" : "failed"), Is.True);
            Assert.That(msg.TextBody.Contains(dataItem.Success ? "succeeded" : "failed"), Is.True);
            Assert.That(msg.BodyParts.Any(bp => bp.ContentDisposition?.Disposition == ContentDisposition.Inline && bp.ContentType.IsMimeType("image", "jpeg")), Is.True);
        });

        MailMergeMessage.DisposeFileStreams(msg);
    }

    [Test]
    public void HtmlStreamAttachments()
    {
        var streamAttachments = new List<StreamAttachment>();
        var text = "Some test for stream attachment";
        var streamAttFilename = "StreamFilename.txt";
        using var mmm = new MailMergeMessage("The subject", "plain text");
        using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text)))
        {
            streamAttachments.Add(new StreamAttachment(stream, streamAttFilename, "text/plain"));
            streamAttachments.Add(new StreamAttachment(stream, streamAttFilename, "text/plain"));
            mmm.StreamAttachments.Add(new StreamAttachment(stream, streamAttFilename, "text/plain"));
        }

        Assert.That(mmm.StreamAttachments.Count == 1, Is.True);
        mmm.StreamAttachments.Clear();
        Assert.That(mmm.StreamAttachments.Count == 0, Is.True);

        mmm.StreamAttachments = streamAttachments;
        Assert.That(mmm.StreamAttachments.Count == 2, Is.True);
    }


    [Test]
    public void HtmlMailMergeWithStreamAttachment()
    {
        var dataItem = new
        {
            Name = "John",
            MailboxAddr = "john@example.com",
            Success = true,
            Date = DateTime.Now,
            SenderAddr = "test@specimen.com"
        };

        var text = "Some test for stream attachment";
        var streamAttFilename = $"StreamFilename_{dataItem.Date:yyyy-MM-dd}.txt";

        using var mmm = MessageFactory.GetHtmlMailWithInlineAndOtherAttachments();
        mmm.FileAttachments.Clear();
        mmm.InlineAttachments.Clear();
        mmm.StreamAttachments.Clear();
        mmm.StringAttachments.Clear();

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text));
        mmm.StreamAttachments.Add(new StreamAttachment(stream, streamAttFilename, "text/plain"));

        var msg = mmm.GetMimeMessage(dataItem);
        var att = msg.Attachments.FirstOrDefault() as MimePart;
        Assert.Multiple(() =>
        {
            Assert.That(att?.FileName == streamAttFilename && att.IsAttachment, Is.True);
            Assert.That(msg.ToString().Contains(text), Is.True);
        });

        MailMergeMessage.DisposeFileStreams(msg);
    }

    [Test]
    public void HtmlMailMergeWithMoreEqualInlineAtt()
    {
        var dataItem = new
        {
            Name = "John",
            MailboxAddr = "john@example.com",
            Success = true,
            Date = DateTime.Now,
            SenderAddr = "test@specimen.com",
            Image = MessageFactory.ImgSuccess
        };

        using var mmm = MessageFactory.GetHtmlMsgWithThreeInlineAttachments();
        var msg = mmm.GetMimeMessage(dataItem);
        var msgFilename = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "test-msg-equal-inline-att.eml"));
        msg.WriteTo(msgFilename);
        Console.WriteLine($"Test mime message saved as {msgFilename}");

        Assert.Multiple(() =>
        {
            Assert.That(new HtmlParser().ParseDocument((string) msg.HtmlBody).All.Count(m => m is IHtmlImageElement) == 3, Is.True);
            Assert.That(msg.BodyParts.Count(bp => bp.ContentDisposition?.Disposition == ContentDisposition.Inline && bp.ContentType.IsMimeType("image", "jpeg")) == 1, Is.True);
        });

        MailMergeMessage.DisposeFileStreams(msg);
    }

    [Test]
    public void AddLinkedResourceManually()
    {
        var dataItem = new
        {
            Name = "John",
            MailboxAddr = "john@example.com",
            Success = true,
            Date = DateTime.Now,
            SenderAddr = "test@specimen.com"
        };

        using var mmm = MessageFactory.GetHtmlMsgWithManualLinkedResources();
        var msg = mmm.GetMimeMessage(dataItem);
        Assert.That(msg.BodyParts.Any(bp => bp.ContentDisposition?.Disposition == ContentDisposition.Inline && bp.ContentType.IsMimeType("image", "jpeg") && bp.ContentId == MessageFactory.MyContentId), Is.True);
        MailMergeMessage.DisposeFileStreams(msg);
    }

    [Test]
    public void DisposeFileStreamsOfMessageAttachments()
    {
        var dataItem = new
        {
            Name = "John",
            MailboxAddr = "john@example.com",
            Success = true,
            Date = DateTime.Now,
            SenderAddr = "test@specimen.com",
            Image = MessageFactory.ImgSuccess
        };

        using (var mmm = MessageFactory.GetHtmlMailWithInlineAndOtherAttachments())
        {
            var mimeMsg = mmm.GetMimeMessage(dataItem);
            foreach (var filename in new[] { MessageFactory.LogFileName, dataItem.Image })
            {
                // file streams are still in use
                Assert.Throws<IOException>(() => File.OpenWrite(Path.Combine(TestFileFolders.FilesAbsPath, filename)));
            }

            // dispose file streams
            MailMergeMessage.DisposeFileStreams(mimeMsg);
        }

        // now all files are fully accessible
        foreach (var filename in new[] { MessageFactory.LogFileName, dataItem.Image })
        {
            var fs = File.OpenWrite(Path.Combine(TestFileFolders.FilesAbsPath, filename));
            fs.Dispose();
        }
    }

    [Test]
    public void IgnoreImgSrcWithEmbeddedBase64Image()
    {
        const string embeddedImage = "data:image/gif;base64,R0lGODlhAQABAIAAAAUEBAAAACwAAAAAAQABAAACAkQBADs=";
        using var mmm = new MailMergeMessage
        {
            // img is 1x1px black
            HtmlText = $"<html><body><img src=\"{embeddedImage}\" width=\"30\" height=\"30\"></body></html>"
        };
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "sender@sample.com"));
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "to@sample.com"));

        var msg = mmm.GetMimeMessage();
        Assert.That(msg.ToString().Contains(embeddedImage), Is.True);
        MailMergeMessage.DisposeFileStreams(msg);
    }

    [Test]
    public void IgnoreImgSrcWithUriTypeHttp()
    {
        const string httpImage = "http://example.com/sample.png";
        using var mmm = new MailMergeMessage
        {
            // img is 1x1px black
            HtmlText = $"<html><base href=\"file:///\" /><body><img src=\"{httpImage}\"></body></html>"
        };
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "sender@sample.com"));
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "to@sample.com"));

        var msg = mmm.GetMimeMessage();
        Assert.That(msg.ToString().Contains(httpImage), Is.True);
        MailMergeMessage.DisposeFileStreams(msg);
    }

    [Test]
    public void ConvertHtmlToPlainText()
    {
        using var mmm = MessageFactory.GetHtmlMessageForHtmlConverter();
        Assert.That(string.IsNullOrEmpty(mmm.PlainText), Is.True);
        mmm.ConvertHtmlToPlainText();
        Assert.That(mmm.PlainText.Length > 0, Is.True);

        mmm.ConvertHtmlToPlainText(new DummyHtmlConverter());
        Assert.That(mmm.PlainText, Is.EqualTo(DummyHtmlConverter.ConstantText));
    }

    [TestCase("{Name} {SenderAddr}", "John test@specimen.com")]
    [TestCase("{Name {SenderAddr}", "{Name {SenderAddr}")] // parsing error
    [TestCase("{NotExisting}", "{NotExisting}")] // formatting error
    [TestCase("", "")]
    public void SearchAndReplace(string text, string expected)
    {
        var dataItem = new
        {
            Name = "John",
            SenderAddr = "test@specimen.com",
        };

        using var mmm = new MailMergeMessage();
        mmm.Config.SmartFormatterConfig.FormatErrorAction = ErrorAction.ThrowError;
        mmm.Config.SmartFormatterConfig.ParseErrorAction = ErrorAction.ThrowError;
        var result = mmm.SearchAndReplaceVars(text, dataItem);
        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("{Name} {SenderAddr}", "John test@specimen.com")]
    [TestCase("{Name {SenderAddr}", "{Name {SenderAddr}")] // parsing error
    [TestCase("{NotExisting}", "{NotExisting}")] // formatting error
    [TestCase("", "")]
    public void SearchAndReplaceFilename(string text, string expected)
    {
        var dataItem = new
        {
            Name = "John",
            SenderAddr = "test@specimen.com",
        };

        using var mmm = new MailMergeMessage();
        mmm.Config.SmartFormatterConfig.FormatErrorAction = ErrorAction.ThrowError;
        mmm.Config.SmartFormatterConfig.ParseErrorAction = ErrorAction.ThrowError;
        var result = mmm.SearchAndReplaceVarsInFilename(text, dataItem);
        Assert.That(result, Is.EqualTo(expected));
    }
}
