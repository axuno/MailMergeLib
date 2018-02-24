using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using MailMergeLib;
using MailMergeLib.MessageStore;
using MailMergeLib.Templates;
using MimeKit;

namespace UnitTests
{
    public class MessageFactory
    {
        public const string HtmlTextFile = "Mailtext.html";
        public const string HtmlConverterTestFile = "TextForHtmlConverter.html";
        public const string HtmlTextThreeInlineAtt = "SameImageSeveralTimes.html";
        public const string PlainTextFile = "Mailtext.txt";
        public const string LogFileName = "LogFile.log";
        public const string ImgSuccess = "success.jpg";
        public const string ImgError = "error.jpg";
        public const string PdfFile = "Sample.pdf";
        public const string Subject = "Logfile for {Date:yyyy-MM-dd}";
        public const string MyContentId = "my.content.id";
        public const string PathRelativeToCodebase = @"..\..\TestFiles\";

        public static MailMergeMessage GetHtmlMailWithInlineAndOtherAttachments()
        {
            var mmm = new MailMergeMessage
            {
                Info = new MessageInfo { Id = 1, Category = "Orders", Description = "Message description", Comments = "Comments to the message", Data = "Data hint" },
                // File.ReadAllText will include \r besides \n, while the internal C# representation is only \n
                HtmlText = string.Join("\n", File.ReadAllLines(Path.Combine(TestFileFolders.FilesAbsPath, HtmlTextFile))), // contains image (<img src="..." />) which must be "inline-attached"
                PlainText = string.Join("\n", File.ReadAllLines(Path.Combine(TestFileFolders.FilesAbsPath, PlainTextFile))),
                Templates = { new Template("Salutation", new Parts { new Part(PartType.Plain, "Hi", "Hi {FirstName}"), new Part(PartType.Plain, "Dear", "Dear {FirstName}"), new Part(PartType.Plain, "Formal", "Dear Sir or Madam") }, "Hi")},
                Subject = Subject,
                Config = { FileBaseDirectory = TestFileFolders.FilesAbsPath, Organization = "MailMergeLib Inc.", CharacterEncoding = Encoding.UTF8, Priority = MessagePriority.Urgent }
            };
            mmm.FileAttachments.Add(new FileAttachment(Path.GetFullPath(Path.Combine(TestFileFolders.FilesAbsPath, LogFileName)), "Log file from {Date:yyyy-MM-dd}.log"));
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "{SenderAddr}"));
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "\"{Name}\" <{MailboxAddr}>", mmm.Config.CharacterEncoding));

            return mmm;
        }

        public static MailMergeMessage GetHtmlMsgWithThreeInlineAttachments()
        {
            var mmm = new MailMergeMessage
            {
                Info = new MessageInfo { Id = 1, Category = "Orders", Description = "Message description", Comments = "Comments to the message", Data = "Data hint"},
                // File.ReadAllText will include \r besides \n, while the internal C# representation is only \n
                HtmlText = string.Join("\n", File.ReadAllLines(Path.Combine(TestFileFolders.FilesAbsPath, HtmlTextThreeInlineAtt))),
                Subject = "Three inline attachments",
                Config = { FileBaseDirectory = TestFileFolders.FilesAbsPath, CharacterEncoding = Encoding.UTF8 }
            };
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "someone@example.com"));
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "somebodyelse@example.com", mmm.Config.CharacterEncoding));

            return mmm;
        }

        public static MailMergeMessage GetHtmlMsgWithManualLinkedResources()
        {
            var mmm = new MailMergeMessage
            {
                HtmlText = $"<html><body><img src=\"cid:{MyContentId}\" width=\"100\"><br/>only an image</body></html>",
                PlainText = "only an image",
                Subject = "Message subject",
                Config = { FileBaseDirectory = TestFileFolders.FilesAbsPath }
            };
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "{SenderAddr}"));
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "\"{Name}\" <{MailboxAddr}>", mmm.Config.CharacterEncoding));
            mmm.AddExternalInlineAttachment(new FileAttachment(Path.GetFullPath(Path.Combine(TestFileFolders.FilesAbsPath, ImgSuccess)), MyContentId));

            return mmm;
        }

        public static MailMergeMessage GetMessageWithAllPropertiesSet()
        {
            var mmm = GetHtmlMailWithInlineAndOtherAttachments();
            mmm.Info = new MessageInfo
            {
                Id = 1,
                Description = "Message description {Date:yyyy-MM-dd}",
                Category = "Some category {Date:yyyy-MM-dd}",
                Comments = "Comments to the message {Date:yyyy-MM-dd}",
                Data = "Data hint {Date:yyyy-MM-dd}"
            };
            mmm.AddExternalInlineAttachment(new FileAttachment(Path.GetFullPath(Path.Combine(TestFileFolders.FilesAbsPath, ImgError)), "error-image.jpg"));
            mmm.FileAttachments.Add(new FileAttachment(Path.GetFullPath(Path.Combine(TestFileFolders.FilesAbsPath, PdfFile)), "information.pdf"));
            mmm.StringAttachments.Add(new StringAttachment("some content", "content.txt"));
            mmm.Headers.Add(HeaderId.Comments, "some comments for header");
            mmm.Config = new MessageConfig()
            {
                FileBaseDirectory = TestFileFolders.FilesAbsPath,
                CharacterEncoding = Encoding.UTF32,
                Organization = "axuno gGmbH",
                TextTransferEncoding = ContentEncoding.SevenBit,
                Priority = MessagePriority.NonUrgent,
                CultureInfo = new CultureInfo("de-DE"),
                BinaryTransferEncoding = ContentEncoding.Base64,
                IgnoreIllegalRecipientAddresses = false,
                IgnoreMissingInlineAttachments = true,
                IgnoreMissingFileAttachments = true,
                StandardFromAddress = new MailboxAddress("from-name", "from-addr@address.com"),
                Xmailer = "MailMergLib-for-UnitTests"
            };

            return mmm;
        }

        public static MailMergeMessage GetHtmlAndPlainMessage_WithTemplates(out Dictionary<string, string> variables)
        {
            var mmm = new MailMergeMessage
            {
                Info = new MessageInfo
                {
                    Id = 1,
                    Description = "Message description",
                    Category = "Message category",
                    Comments = "Comments to the message",
                    Data = "Data hint"
                },
                HtmlText = "<html><head></head><body>{:template(Salutation)}and so on</body></html>",
                PlainText = "{:template(Salutation)}and so on...",
                Templates =
                {
                    new Template("Salutation",
                        new Parts
                        {
                            new Part(PartType.Plain, "Hi", "Hi {FirstName}"),
                            new Part(PartType.Html, "Hi", "Hi <b>{FirstName}</b><br>"),
                            new Part(PartType.Plain, "Dear", "Dear {FirstName}"),
                            new Part(PartType.Html, "Dear", "Dear <b>{FirstName}</b><br>"),
                            new Part(PartType.Plain, "Formal", "Dear Sir or Madam"),
                            new Part(PartType.Html, "Formal", "<b>Dear Sir or Madam</b><br>"),
                        }, "Formal")
                },
                Subject = "Message to {FirstName}",
                Config =
                {
                    Organization = "MailMergeLib Inc.",
                    CharacterEncoding = Encoding.UTF8,
                }
            };
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "{FromAddr}"));
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "{ToAddr}"));
            variables = new Dictionary<string, string>() { { "FirstName", "Jane" }, { "FromAddr", "from@example.com" }, { "ToAddr", "to@example.com" } };

            return mmm;
        }

        public static MailMergeMessage GetHtmlMessageForHtmlConverter()
        {
            var mmm = new MailMergeMessage
            {
                HtmlText = string.Join("\n", File.ReadAllLines(Path.Combine(TestFileFolders.FilesAbsPath, HtmlConverterTestFile))),
                Subject = "HTML Converter Test",
                Config =
                {
                    Organization = "MailMergeLib Inc.",
                    CharacterEncoding = Encoding.UTF8,
                }
            };
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "from@test.com"));
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "to@test.com"));

            return mmm;
        }
    }
}
