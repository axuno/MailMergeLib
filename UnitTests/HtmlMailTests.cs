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
		const string _pathRelativeToCodebase = @"..\..\TestFiles\";

		[Test]
		public void HtmlMailMergeWithInlineAndAtt()
		{
			var testDataAbsPath = Path.Combine(Helper.GetCodeBaseDirectory(), _pathRelativeToCodebase);

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
				HtmlText = File.ReadAllText(Path.Combine(testDataAbsPath, _htmlTextFile)), // contains image (<img src="..." />) which must be "inline-attached"
				PlainText = File.ReadAllText(Path.Combine(testDataAbsPath, _plainTextFile)),
				Subject = _subject,
				Config = {FileBaseDirectory = testDataAbsPath, Organization = "MailMergeLib Inc.", CharacterEncoding = Encoding.UTF8, Priority = MessagePriority.Urgent }
			};
			mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "{SenderAddr}"));
			mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "\"{Name}\" <{MailboxAddr}>", mmm.Config.CharacterEncoding));
			mmm.FileAttachments.Add(new FileAttachment(Path.Combine(testDataAbsPath, _logFileName), "Log file from {Date:yyyy-MM-dd}.log"));
			
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
		}
	}
}
