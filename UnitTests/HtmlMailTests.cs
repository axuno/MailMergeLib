using System;
using System.IO;
using System.Linq;
using System.Text;
using MailMergeLib;
using MimeKit;
using NUnit.Framework;

namespace UnitTests
{
	[TestFixture]
	public class HtmlMailTests
    {
		const string _htmlTextFile = "Mailtext.html";
		const string _plainTextFile = "Mailtext.txt";
		const string _logFileName = "LogFile.log";
		const string _subject = "Logfile for {Date:yyyy-MM-dd}";
		const string _relDirectoryPath = @"..\..\TestFiles\";

		[Test]
		public void HtmlMailMergeWithInlineAndAtt()
		{
			const string logfileName = "Log file from {Date:yyyy-MM-dd}.log";
			var currAbsPath = Path.Combine(Environment.CurrentDirectory, _relDirectoryPath);

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
				HtmlText = File.ReadAllText(Path.Combine(currAbsPath, _htmlTextFile)), // contains image (<img src="..." />) which must be "inline-attached"
				PlainText = File.ReadAllText(Path.Combine(currAbsPath, _plainTextFile)),
				Subject = _subject,
				Config = {FileBaseDirectory = currAbsPath, Organization = "MailMergeLib Inc.", CharacterEncoding = Encoding.UTF8, Priority = MessagePriority.Urgent }
			};
			mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "{SenderAddr}"));
			mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "\"{Name}\" <{MailboxAddr}>", mmm.Config.CharacterEncoding));
			mmm.FileAttachments.Add(new FileAttachment(Path.Combine(currAbsPath, _logFileName), logfileName));
			
			var msg = mmm.GetMimeMessage(dataItem);
			msg.WriteTo(@"C:\temp\msg.eml");

			Assert.IsTrue(((MailboxAddress) msg.From.First()).Address == dataItem.SenderAddr);
			Assert.IsTrue(((MailboxAddress)msg.To.First()).Address == dataItem.MailboxAddr);
			Assert.IsTrue(((MailboxAddress)msg.To.First()).Name == dataItem.Name);
			Assert.IsTrue(msg.Headers[HeaderId.Organization] == mmm.Config.Organization);
			Assert.IsTrue(msg.Priority == mmm.Config.Priority);
			Assert.IsTrue(msg.Attachments.FirstOrDefault(a => ((MimePart)a).FileName == logfileName.Replace("{Date:yyyy-MM-dd}", dataItem.Date.ToString("yyyy-MM-dd"))) != null);
			Assert.IsTrue(msg.Subject == _subject.Replace("{Date:yyyy-MM-dd}",dataItem.Date.ToString("yyyy-MM-dd")));
			Assert.IsTrue(msg.HtmlBody.Contains(dataItem.Success ? "succeeded" : "failed"));
			Assert.IsTrue(msg.TextBody.Contains(dataItem.Success ? "succeeded" : "failed"));
			Assert.IsTrue(msg.BodyParts.Any(bp => bp.ContentDisposition?.Disposition == ContentDisposition.Inline && bp.ContentType.IsMimeType("image", "jpeg")));
		}
	}
}
