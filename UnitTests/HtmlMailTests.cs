using System;
using System.IO;
using System.Linq;
using System.Text;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using MailMergeLib;
using MimeKit;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class HtmlMailTests
    {
        private const string _htmlTextFile = "Mailtext.html";
        private const string _htmlTextThreeInlineAtt = "SameImageSeveralTimes.html";
        private const string _plainTextFile = "Mailtext.txt";
        private const string _imgSuccess = "success.jpg";
        private const string _logFileName = "LogFile.log";
        private const string _subject = "Logfile for {Date:yyyy-MM-dd}";
        private const string _pathRelativeToCodebase = @"..\..\TestFiles\";

        [Test]
        public void HtmlMailMergeWithInlineAndAtt()
        {
            var filesAbsPath = Path.Combine(Helper.GetCodeBaseDirectory(), _pathRelativeToCodebase);

            var dataItem = new
            {
                Name = "John",
                MailboxAddr = "john@example.com",
                Success = true,
                Date = DateTime.Now,
                SenderAddr = "test@specimen.com"
            };

            var mmm = new MailMergeMessage
            {
                HtmlText = File.ReadAllText(Path.Combine(filesAbsPath, _htmlTextFile)), // contains image (<img src="..." />) which must be "inline-attached"
                PlainText = File.ReadAllText(Path.Combine(filesAbsPath, _plainTextFile)),
                Subject = _subject,
                Config = {FileBaseDirectory = filesAbsPath, Organization = "MailMergeLib Inc.", CharacterEncoding = Encoding.UTF8, Priority = MessagePriority.Urgent }
            };
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "{SenderAddr}"));
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "\"{Name}\" <{MailboxAddr}>", mmm.Config.CharacterEncoding));
            mmm.FileAttachments.Add(new FileAttachment(Path.Combine(filesAbsPath, _logFileName), "Log file from {Date:yyyy-MM-dd}.log"));
            
            var msg = mmm.GetMimeMessage(dataItem);
            var msgFilename = Path.Combine(Path.GetTempPath(), "test-msg.eml");
            msg.WriteTo(msgFilename);
            Console.WriteLine($"Test mime message saved as {msgFilename}");
            
            Assert.IsTrue(((MailboxAddress) msg.From.First()).Address == dataItem.SenderAddr);
            Assert.IsTrue(((MailboxAddress)msg.To.First()).Address == dataItem.MailboxAddr);
            Assert.IsTrue(((MailboxAddress)msg.To.First()).Name == dataItem.Name);
            Assert.IsTrue(msg.Headers[HeaderId.Organization] == mmm.Config.Organization);
            Assert.IsTrue(msg.Priority == mmm.Config.Priority);
            Assert.IsTrue(msg.Attachments.FirstOrDefault(a => ((MimePart)a).FileName == "Log file from {Date:yyyy-MM-dd}.log".Replace("{Date:yyyy-MM-dd}", dataItem.Date.ToString("yyyy-MM-dd"))) != null);
            Assert.IsTrue(msg.Subject == _subject.Replace("{Date:yyyy-MM-dd}",dataItem.Date.ToString("yyyy-MM-dd")));
            Assert.IsTrue(msg.HtmlBody.Contains(dataItem.Success ? "succeeded" : "failed"));
            Assert.IsTrue(msg.TextBody.Contains(dataItem.Success ? "succeeded" : "failed"));
            Assert.IsTrue(msg.BodyParts.Any(bp => bp.ContentDisposition?.Disposition == ContentDisposition.Inline && bp.ContentType.IsMimeType("image", "jpeg")));

            MailMergeMessage.DisposeFileStreams(msg);
        }

        [Test]
        public void HtmlMailMergeWithMoreEqualInlineAtt()
        {
            var filesAbsPath = Path.Combine(Helper.GetCodeBaseDirectory(), _pathRelativeToCodebase);

            var dataItem = new
            {
                Image = _imgSuccess
            };

            var mmm = new MailMergeMessage
            {
                HtmlText = File.ReadAllText(Path.Combine(filesAbsPath, _htmlTextThreeInlineAtt)),
                Subject = "Three inline attachments",
                Config = { FileBaseDirectory = filesAbsPath, CharacterEncoding = Encoding.UTF8 }
            };
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "someone@example.com"));
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "somebodyelse@example.com", mmm.Config.CharacterEncoding));

            var msg = mmm.GetMimeMessage(dataItem);
            var msgFilename = Path.Combine(Path.GetTempPath(), "test-msg-equal-inline-att.eml");
            msg.WriteTo(msgFilename);
            Console.WriteLine($"Test mime message saved as {msgFilename}");
            
            Assert.IsTrue(new HtmlParser().Parse(msg.HtmlBody).All.Count(m => m is IHtmlImageElement) == 3);
            Assert.IsTrue(msg.BodyParts.Count(bp => bp.ContentDisposition?.Disposition == ContentDisposition.Inline && bp.ContentType.IsMimeType("image", "jpeg")) == 1);

            MailMergeMessage.DisposeFileStreams(msg);
        }

        [Test]
        public void AddLinkedResourceManually()
        {
            const string myContentId = "my.content.id";
            var filesAbsPath = Path.Combine(Helper.GetCodeBaseDirectory(), _pathRelativeToCodebase);

            var dataItem = new
            {
                Name = "John",
                MailboxAddr = "john@example.com",
                Success = true,
                Date = DateTime.Now,
                SenderAddr = "test@specimen.com"
            };

            var mmm = new MailMergeMessage
            {
                HtmlText = $"<html><body><img src=\"cid:{myContentId}\" width=\"100\"><br/>only an image</body></html>",
                PlainText = "only an image",
                Subject = "Message subject",
                Config = { FileBaseDirectory = filesAbsPath }
            };
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "{SenderAddr}"));
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "\"{Name}\" <{MailboxAddr}>", mmm.Config.CharacterEncoding));
            mmm.AddExternalInlineAttachment(new FileAttachment(Path.Combine(filesAbsPath, _imgSuccess), myContentId));

            var msg = mmm.GetMimeMessage(dataItem);
            Assert.IsTrue(msg.BodyParts.Any(bp => bp.ContentDisposition?.Disposition == ContentDisposition.Inline && bp.ContentType.IsMimeType("image", "jpeg") && bp.ContentId == myContentId));
            MailMergeMessage.DisposeFileStreams(msg);
        }

        [Test]
        public void DisposeFileStreamsOfMessageAttachments()
        {
            var dataItem = new
            {
                Image = _imgSuccess
            };

            var filesAbsPath = Path.Combine(Helper.GetCodeBaseDirectory(), _pathRelativeToCodebase);
            var mmm = new MailMergeMessage
            {
                HtmlText = File.ReadAllText(Path.Combine(filesAbsPath, _htmlTextThreeInlineAtt)),
                Subject = "Dispose file streams",
                Config = { FileBaseDirectory = filesAbsPath }
            };
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "from@example.com", mmm.Config.CharacterEncoding));
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "to@example.com", mmm.Config.CharacterEncoding));
            mmm.FileAttachments.Add(new FileAttachment(Path.Combine(filesAbsPath, _logFileName), "Log file.log"));

            var mimeMsg = mmm.GetMimeMessage(dataItem);
            foreach (var filename in new[] {_logFileName, dataItem.Image })
            {
                // file streams are still in use
                Assert.Throws<IOException>(() => File.OpenWrite(Path.Combine(filesAbsPath, filename)));
            }

            // dispose file streams
            MailMergeMessage.DisposeFileStreams(mimeMsg);

            // now all files are fully accessible
            foreach (var filename in new[] { _logFileName, dataItem.Image })
            {
                var fs = File.OpenWrite(Path.Combine(filesAbsPath, filename));
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
    }
}
