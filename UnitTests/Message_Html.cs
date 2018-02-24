using System;
using System.IO;
using System.Linq;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using MailMergeLib;
using MimeKit;
using NUnit.Framework;

namespace UnitTests
{
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

            var mmm = MessageFactory.GetHtmlMailWithInlineAndOtherAttachments();
            
            var msg = mmm.GetMimeMessage(dataItem);
            var msgFilename = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "test-msg.eml"));
            msg.WriteTo(msgFilename);
            Console.WriteLine($"Test mime message saved as {msgFilename}");
            
            Assert.IsTrue(((MailboxAddress) msg.From.First()).Address == dataItem.SenderAddr);
            Assert.IsTrue(((MailboxAddress)msg.To.First()).Address == dataItem.MailboxAddr);
            Assert.IsTrue(((MailboxAddress)msg.To.First()).Name == dataItem.Name);
            Assert.IsTrue(msg.Headers[HeaderId.Organization] == mmm.Config.Organization);
            Assert.IsTrue(msg.Priority == mmm.Config.Priority);
            Assert.IsTrue(msg.Attachments.FirstOrDefault(a => ((MimePart)a).FileName == "Log file from {Date:yyyy-MM-dd}.log".Replace("{Date:yyyy-MM-dd}", dataItem.Date.ToString("yyyy-MM-dd"))) != null);
            Assert.IsTrue(msg.Subject == mmm.Subject.Replace("{Date:yyyy-MM-dd}",dataItem.Date.ToString("yyyy-MM-dd")));
            Assert.IsTrue(msg.HtmlBody.Contains(dataItem.Success ? "succeeded" : "failed"));
            Assert.IsTrue(msg.TextBody.Contains(dataItem.Success ? "succeeded" : "failed"));
            Assert.IsTrue(msg.BodyParts.Any(bp => bp.ContentDisposition?.Disposition == ContentDisposition.Inline && bp.ContentType.IsMimeType("image", "jpeg")));

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

            var mmm = MessageFactory.GetHtmlMsgWithThreeInlineAttachments();

            var msg = mmm.GetMimeMessage(dataItem);
            var msgFilename = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "test-msg-equal-inline-att.eml"));
            msg.WriteTo(msgFilename);
            Console.WriteLine($"Test mime message saved as {msgFilename}");
            
            Assert.IsTrue(new HtmlParser().Parse(msg.HtmlBody).All.Count(m => m is IHtmlImageElement) == 3);
            Assert.IsTrue(msg.BodyParts.Count(bp => bp.ContentDisposition?.Disposition == ContentDisposition.Inline && bp.ContentType.IsMimeType("image", "jpeg")) == 1);

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

            var mmm = MessageFactory.GetHtmlMsgWithManualLinkedResources();

            var msg = mmm.GetMimeMessage(dataItem);
            Assert.IsTrue(msg.BodyParts.Any(bp => bp.ContentDisposition?.Disposition == ContentDisposition.Inline && bp.ContentType.IsMimeType("image", "jpeg") && bp.ContentId == MessageFactory.MyContentId));
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

            var mmm = MessageFactory.GetHtmlMailWithInlineAndOtherAttachments();

            var mimeMsg = mmm.GetMimeMessage(dataItem);
            foreach (var filename in new[] { MessageFactory.LogFileName, dataItem.Image })
            {
                // file streams are still in use
                Assert.Throws<IOException>(() => File.OpenWrite(Path.Combine(TestFileFolders.FilesAbsPath, filename)));
            }

            // dispose file streams
            MailMergeMessage.DisposeFileStreams(mimeMsg);

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
            var mmm = new MailMergeMessage
            {
                // img is 1x1px black
                HtmlText = $"<html><body><img src=\"{embeddedImage}\" width=\"30\" height=\"30\"></body></html>"
            };
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "sender@sample.com"));
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "to@sample.com"));

            var msg = mmm.GetMimeMessage();
            Assert.IsTrue(msg.ToString().Contains(embeddedImage));
            MailMergeMessage.DisposeFileStreams(msg);
        }

        [Test]
        public void IgnoreImgSrcWithUriTypeHttp()
        {
            const string httpImage = "http://example.com/sample.png";
            var mmm = new MailMergeMessage
            {
                // img is 1x1px black
                HtmlText = $"<html><base href=\"file:///\" /><body><img src=\"{httpImage}\"></body></html>"
            };
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "sender@sample.com"));
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "to@sample.com"));

            var msg = mmm.GetMimeMessage();
            Assert.IsTrue(msg.ToString().Contains(httpImage));
            MailMergeMessage.DisposeFileStreams(msg);
        }

        [Test]
        public void ConvertHtmlToPlainText()
        {
            var mmm = MessageFactory.GetHtmlMessageForHtmlConverter();
            Assert.IsTrue(string.IsNullOrEmpty(mmm.PlainText));
            mmm.ConvertHtmlToPlainText();
            Assert.IsTrue(mmm.PlainText.Length > 0);

            mmm.ConvertHtmlToPlainText(new DummyHtmlConverter());
            Assert.AreEqual(DummyHtmlConverter.ConstantText, mmm.PlainText);
        }
    }
}
